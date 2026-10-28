# FinPay Core Engine — Concepts, Architecture & Interview Guide

> **A Comprehensive Technical Breakdown for Junior .NET Backend Engineers**  
> This guide explains **every core concept, design decision, and pattern** used in this project, why it was chosen, where it lives in the codebase, and how to explain it in technical interviews.

---

## Table of Contents
1. [C# Language & Object-Oriented Programming (OOP) Deep Dive](#1-c-language--object-oriented-programming-oop-deep-dive)
   * [1.1 Classes, Objects & Constructors](#11-classes-objects--constructors)
   * [1.2 Access Modifiers & Data Hiding](#12-access-modifiers--data-hiding)
   * [1.3 Properties & Encapsulation](#13-properties--encapsulation)
   * [1.4 Inheritance & Base Classes](#14-inheritance--base-classes)
   * [1.5 Polymorphism & Dynamic Dispatch](#15-polymorphism--dynamic-dispatch)
   * [1.6 Modern C# Records & Immutable DTOs](#16-modern-c-records--immutable-dtos)
   * [1.7 Enums & Eliminating Magic Strings](#17-enums--eliminating-magic-strings)
   * [1.8 Nullable Reference Types](#18-nullable-reference-types)
   * [1.9 Async / Await & Non-Blocking I/O](#19-async--await--non-blocking-io)
   * [1.10 Custom Exception Hierarchy](#110-custom-exception-hierarchy)
   * [1.11 Generics & Reusable Wrappers](#111-generics--reusable-wrappers)
   * [1.12 LINQ (Language Integrated Query)](#112-linq-language-integrated-query)
2. [Database & Entity Framework Core (EF Core) Deep Dive](#2-database--entity-framework-core-ef-core-deep-dive)
   * [2.1 Relational Schema & Table Design](#21-relational-schema--table-design)
   * [2.2 Primary Keys: Guid vs. BigInt](#22-primary-keys-guid-vs-bigint)
   * [2.3 Foreign Keys & Entity Relationships](#23-foreign-keys--entity-relationships)
   * [2.4 Restrict Deletion vs. Cascade Delete](#24-restrict-deletion-vs-cascade-delete)
   * [2.5 Database Indexing & Query Performance](#25-database-indexing--query-performance)
   * [2.6 Code-First Migrations](#26-code-first-migrations)
   * [2.7 Fluent API Configuration](#27-fluent-api-configuration)
   * [2.8 Table-Per-Hierarchy (TPH) Inheritance Mapping](#28-table-per-hierarchy-tph-inheritance-mapping)
   * [2.9 Optimistic Concurrency with SQL Server RowVersion](#29-optimistic-concurrency-with-sql-server-rowversion)
   * [2.10 Strict Double-Entry Ledger & ACID Transactions](#210-strict-double-entry-ledger--acid-transactions)
   * [2.11 Read-Only Optimization with AsNoTracking()](#211-read-only-optimization-with-asnotracking)
3. [ASP.NET Core Web API & Clean Architecture](#3-aspnet-core-web-api--clean-architecture)
   * [3.1 Clean Architecture (4-Layer Inward Dependency)](#31-clean-architecture-4-layer-inward-dependency)
   * [3.2 Dependency Injection (DI) Lifetimes](#32-dependency-injection-di-lifetimes)
   * [3.3 The Middleware Pipeline](#33-the-middleware-pipeline)
   * [3.4 Global Exception Handling Middleware](#34-global-exception-handling-middleware)
   * [3.5 JWT Bearer Authentication & Role-Based Authorization](#35-jwt-bearer-authentication--role-based-authorization)
   * [3.6 Thin Controllers vs. Service Layer](#36-thin-controllers-vs-service-layer)
   * [3.7 The DTO (Data Transfer Object) Pattern](#37-the-dto-data-transfer-object-pattern)
   * [3.8 OpenAPI / Swagger Integration](#38-openapi--swagger-integration)
4. [Unit & Relational Integration Testing](#4-unit--relational-integration-testing)
   * [4.1 Unit Testing with xUnit & Moq](#41-unit-testing-with-xunit--moq)
   * [4.2 Relational Integration Testing with SQL Server LocalDB](#42-relational-integration-testing-with-sql-server-localdb)
5. [Interview Questions & How to Answer Them](#5-interview-questions--how-to-answer-them)

---

## 1. C# Language & Object-Oriented Programming (OOP) Deep Dive

### 1.1 Classes, Objects & Constructors
* **Concept:** A `class` is a blueprint that defines state (fields/properties) and behavior (methods). An `object` is an instantiated instance of that blueprint in memory.
* **In this Project:**
  * [`User`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Entities/User.cs): Represents an identity account.
  * [`WalletBase`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Entities/WalletBase.cs): Represents a financial digital wallet.
* **Why Parameterless Constructors for EF Core?**
  * Notice entities have a `protected User() { }` constructor and a public parameterized constructor `public User(string fullName, string email, ...)`.
  * **Why?** EF Core requires a parameterless constructor to instantiate entities when reading data back from SQL Server via reflection. Making it `protected` prevents client code from creating an empty, unvalidated user object, while still allowing EF Core and derived classes to instantiate it.

### 1.2 Access Modifiers & Data Hiding
* **Access Modifiers Used:**
  * `public`: Accessible from any assembly (e.g. `TransferFundsAsync`).
  * `private`: Accessible only within the enclosing class (e.g. `ValidateWalletNumber`).
  * `protected`: Accessible within the enclosing class and any derived class (e.g. `protected set` on `WalletBase.Balance`).
  * `internal`: Accessible only within the same assembly (`FinPay.Domain`).
* **Why?** By restricting access, we guarantee that other layers (like API controllers) cannot bypass domain validations or corrupt internal states.

### 1.3 Properties & Encapsulation
* **Concept:** Encapsulation means bundling data with the methods that operate on that data and restricting direct access to object internals.
* **In this Project:**
  ```csharp
  // Located in backend/src/FinPay.Domain/Entities/WalletBase.cs
  public decimal Balance { get; protected set; }
  ```
  * In a naive CRUD application, developers write `wallet.Balance -= 500;` directly in a controller. This is catastrophic in financial software because a developer might forget to check if `balance >= amount`, or forget to log a ledger entry.
  * In FinPay, the `set` accessor is `protected`. The **only** way to alter the balance is through domain methods:
  ```csharp
  public virtual void Debit(decimal amount)
  {
      if (amount <= 0)
          throw new DomainValidationException($"Debit amount must be greater than zero. Received: {amount}.");

      if (IsFrozen)
          throw new WalletFrozenException(WalletNumber);

      if (Balance < amount)
          throw new InsufficientFundsException(Balance, amount);

      Balance -= amount; // Non-negative balance invariant guaranteed!
  }
  ```

### 1.4 Inheritance & Base Classes
* **Concept:** Inheritance allows a derived class to inherit state and behavior from a parent class, promoting code reuse and establishing an *is-a* relationship.
* **In this Project:**
  * [`BaseEntity`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Common/BaseEntity.cs): Provides the `Guid Id` primary key.
  * [`WalletBase`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Entities/WalletBase.cs): Inherits from `BaseEntity`. Defines common wallet properties (`UserId`, `WalletNumber`, `Balance`, `IsFrozen`, `RowVersion`).
  * [`BasicWallet`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Entities/BasicWallet.cs): Inherits from `WalletBase`.
  * [`MerchantWallet`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Entities/MerchantWallet.cs): Inherits from `WalletBase`.
  * **The `base` keyword:**
    ```csharp
    public BasicWallet(Guid userId, string walletNumber, decimal initialBalance = 0m)
        : base(userId, walletNumber, WalletType.Basic, initialBalance)
    {
    }
    ```
    This invokes the parent `WalletBase` constructor, ensuring that 10-digit validation and non-negative initial balance checks run consistently.

### 1.5 Polymorphism & Dynamic Dispatch
* **Concept:** Polymorphism allows objects of different derived types to be treated as instances of their common base class, while executing type-specific behavior at runtime.
* **In this Project:**
  * In `WalletBase`:
    ```csharp
    public abstract decimal DailyTransferLimit { get; }
    public abstract decimal CalculateFee(decimal amount);
    ```
  * In `BasicWallet`:
    ```csharp
    public override decimal DailyTransferLimit => 50_000m; // PKR 50,000 limit
    public override decimal CalculateFee(decimal amount) => 0m; // 0% fee on P2P
    ```
  * In `MerchantWallet`:
    ```csharp
    public override decimal DailyTransferLimit => 1_000_000m; // PKR 1,000,000 limit
    public override decimal CalculateFee(decimal amount)
    {
        if (amount <= 0) return 0m;
        // 0.5% incoming service fee rounded away from zero
        return Math.Round(amount * 0.005m, 2, MidpointRounding.AwayFromZero);
    }
    ```
  * **Why is this elegant?** In `TransferService`, the code does **not** write ugly `if (wallet is BasicWallet) ... else if (wallet is MerchantWallet) ...`. Instead, it simply calls:
    ```csharp
    var fee = receiverWallet.CalculateFee(request.Amount);
    ```
    The .NET runtime automatically invokes the correct calculation via virtual method table (vtable) dynamic dispatch!

### 1.6 Modern C# Records & Immutable DTOs
* **Concept:** C# `record` types provide immutable, value-based equality semantics out of the box.
* **In this Project:**
  ```csharp
  // Located in backend/src/FinPay.Application/DTOs/Transfer/TransferRequestDto.cs
  public record TransferRequestDto(
      string ReceiverWalletNumber,
      decimal Amount,
      string? Notes = null);
  ```
* **Why records for DTOs?**
  1. **Immutability:** Once an incoming HTTP request is deserialized, no rogue method can silently alter the requested transfer amount.
  2. **Value Equality:** Two records with identical property values evaluate as equal (`dto1 == dto2`), which makes unit testing assertions clean and predictable.

### 1.7 Enums & Eliminating Magic Strings
* **Concept:** Strongly-typed enumerations prevent invalid values and typos from entering business logic.
* **In this Project:**
  * [`UserRole`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Enums/UserRole.cs): `Customer = 1`, `Admin = 2`
  * [`WalletType`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Enums/WalletType.cs): `Basic = 1`, `Merchant = 2`
  * [`TransactionStatus`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Enums/TransactionStatus.cs): `Pending = 1`, `Completed = 2`, `Failed = 3`
  * [`LedgerEntryType`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Domain/Enums/LedgerEntryType.cs): `Debit = 1`, `Credit = 2`
* **Why?** Writing `status == "completed"` in multiple files leads to bugs if someone writes `"Completed"` or `"COMPLETED"`. Enums enforce compile-time safety.

### 1.8 Nullable Reference Types
* **Concept:** Enabled via `<Nullable>enable</Nullable>` in `.csproj`. Tells the C# compiler to warn you if a reference variable could be `null`.
* **In this Project:**
  * `Transaction.SenderWalletId` is `Guid?` (nullable Guid) because system-originated deposits have no sender wallet.
  * `Transaction.ReceiverWalletId` is `Guid` (non-nullable Guid) because every transaction **must** have a recipient.

### 1.9 Async / Await & Non-Blocking I/O
* **Concept:** Frees the web server thread pool to handle other incoming HTTP requests while waiting for database or network I/O to complete.
* **In this Project:**
  ```csharp
  public async Task<TransferResponseDto> TransferFundsAsync(
      Guid senderUserId, TransferRequestDto request, CancellationToken cancellationToken)
  ```
  * Uses `await _walletRepository.GetByUserIdAsync(...)` and `await _context.SaveChangesAsync(...)`.
  * Passes `CancellationToken` down to EF Core queries so that if a client closes their browser mid-request, the database query can be cancelled immediately to conserve server resources.

### 1.10 Custom Exception Hierarchy
* **Concept:** Instead of throwing generic `Exception("something failed")`, domain and application code throws domain-specific exceptions.
* **In this Project:**
  * `InsufficientFundsException`: Carries `AvailableBalance` and `RequestedAmount`.
  * `WalletFrozenException`: Carries `WalletNumber`.
  * `SelfTransferException`: Thrown when sender attempts to transfer to own wallet.
  * `DailyLimitExceededException`: Carries `DailyLimit`, `CurrentDayTotal`, and `RequestedAmount`.
  * `ConcurrencyException`: Thrown when two conflicting concurrent transfers collide.
* **Why?** The global middleware can catch each specific exception and map it to the correct HTTP status code (`400 Bad Request`, `409 Conflict`, `404 Not Found`).

### 1.11 Generics & Reusable Wrappers
* **Concept:** Generics allow defining classes and methods with placeholder type parameters `<T>`.
* **In this Project:**
  * [`ApiResponse<T>`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Application/Common/ApiResponse.cs): Uniform API response containing `bool Success`, `string Message`, `T Data`, `string ErrorCode`, `DateTime Timestamp`.
  * [`PagedResult<T>`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Application/Common/PagedResult.cs): Reusable pagination container with `IReadOnlyList<T> Items`, `TotalCount`, `PageNumber`, `PageSize`, and computed `TotalPages`.

### 1.12 LINQ (Language Integrated Query)
* **Concept:** Declarative query syntax for filtering, projecting, and aggregating data collections.
* **In this Project (`TransactionRepository.cs`):**
  * `.Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId)`: Filters by wallet ownership.
  * `.OrderByDescending(t => t.CreatedAt)`: Sorts newest transactions first.
  * `.Skip((pageNumber - 1) * pageSize).Take(pageSize)`: Efficient SQL Server `OFFSET ... FETCH NEXT` pagination.
  * `.SumAsync(t => (decimal?)t.Amount)`: Aggregates daily transfer velocity inside the database engine.
  * `.AnyAsync(u => u.Email == normalized)`: Fast SQL `EXISTS` check.

---

## 2. Database & Entity Framework Core (EF Core) Deep Dive

### 2.1 Relational Schema & Table Design
FinPay implements 4 core relational tables:
1. **`Users`**: Holds identity, hashed passwords, roles, and timestamps.
2. **`Wallets`**: Holds financial balances, wallet numbers, account types, freeze flags, and concurrency tokens.
3. **`Transactions`**: Header record for financial operations (Reference ID, Sender ID, Receiver ID, Amount, Fee, Status).
4. **`LedgerEntries`**: Double-entry ledger audit trail rows (Transaction ID, Wallet ID, Debit/Credit, Amount, Balance After).

### 2.2 Primary Keys: Guid vs. BigInt
* **`Users`, `Wallets`, `Transactions` use `Guid` (`uniqueidentifier` in SQL Server):**
  * **Why?** Guids can be generated in C# application memory (`Guid.NewGuid()`) before saving to the database. This allows creating complex entity graphs (e.g. creating a `Transaction` and referencing its ID in two `LedgerEntry` rows) in a single in-memory batch without waiting for the database to return an identity integer.
* **`LedgerEntries` uses `long` (`bigint` identity autoincrement):**
  * **Why?** High-volume ledger tables can have millions of rows. A 64-bit clustered integer index provides compact storage, predictable chronological ordering, and prevents index fragmentation in B-Trees.

### 2.3 Foreign Keys & Entity Relationships
* **1-to-1 Relationship (`User` <-> `Wallet`):**
  * Every User has exactly one Wallet.
  * Configured via Fluent API:
    ```csharp
    builder.HasOne(u => u.Wallet)
           .WithOne(w => w.User)
           .HasForeignKey<WalletBase>(w => w.UserId)
           .OnDelete(DeleteBehavior.Restrict);
    ```
* **1-to-Many Relationship (`Wallet` <-> `Transactions`):**
  * A wallet has many `SentTransactions` and many `ReceivedTransactions`.
* **1-to-Many Relationship (`Transaction` <-> `LedgerEntries`):**
  * One transaction links to multiple ledger entries (2 for a P2P transfer, 1 for a deposit).

### 2.4 Restrict Deletion vs. Cascade Delete
* **Why `DeleteBehavior.Restrict`?**
  * In standard CRUD apps, developers use `CascadeDelete` so deleting a user deletes their orders.
  * **In banking/financial software, Cascade Delete is strictly forbidden!** Deleting a user must NEVER delete transaction history or ledger entries. Financial law requires retaining transaction history. With `Restrict`, attempting to delete an entity that has active foreign key references throws an error.

### 2.5 Database Indexing & Query Performance
* **Unique Indexes:**
  * `Users.Email`: Prevents duplicate email registrations at the database level.
  * `Users.PhoneNumber`: Prevents duplicate phone numbers.
  * `Wallets.WalletNumber`: Guarantees exactly one wallet possesses a given 10-digit number.
  * `Transactions.ReferenceId`: Guarantees transaction reference numbers are globally unique.
* **Non-Unique Filter Indexes:**
  * `Transactions.CreatedAt` and `LedgerEntries.Timestamp`: Enables fast sorting and range scans when querying monthly or daily statements.
  * `LedgerEntries.WalletId`: Allows querying all ledger lines for a specific wallet in milliseconds.

### 2.6 Code-First Migrations
* **Concept:** Schema changes are authored in C# entity classes and configurations, and EF Core automatically generates SQL migration scripts.
* **In this Project:**
  * Migration file: [`20261028144334_InitialCreate.cs`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.Infrastructure/Persistence/Migrations/20261028144334_InitialCreate.cs).
  * Executed using the `dotnet-ef` CLI tool:
    ```powershell
    dotnet tool run dotnet-ef database update --project backend/src/FinPay.Infrastructure --startup-project backend/src/FinPay.API
    ```

### 2.7 Fluent API Configuration
* **Concept:** Instead of cluttering domain entities with database attributes (`[Required]`, `[MaxLength]`), all persistence concerns are configured in separate configuration classes implementing `IEntityTypeConfiguration<T>`.
* **Why?** Maintains pure domain entities free from database dependencies (Separation of Concerns).

### 2.8 Table-Per-Hierarchy (TPH) Inheritance Mapping
* **Concept:** Maps an inheritance hierarchy (`WalletBase`, `BasicWallet`, `MerchantWallet`) into a **single** database table (`Wallets`).
* **In this Project (`WalletConfiguration.cs`):**
  ```csharp
  builder.HasDiscriminator<WalletType>("WalletType")
         .HasValue<BasicWallet>(WalletType.Basic)
         .HasValue<MerchantWallet>(WalletType.Merchant);
  ```
* **Why TPH?**
  * We avoid expensive SQL `JOIN` operations that occur with Table-Per-Type (TPT).
  * EF Core automatically filters and instantiates the correct derived C# class (`BasicWallet` or `MerchantWallet`) based on the discriminator column `WalletType`!

### 2.9 Optimistic Concurrency with SQL Server RowVersion
* **The Problem (Race Condition / Double Spend):**
  * User has PKR 1,000.
  * Two transfer requests for PKR 800 arrive simultaneously on two separate threads at the exact same millisecond.
  * Both threads read `Balance = 1000`. Both pass `Balance >= 800`.
  * Both execute `Debit(800)`.
  * Without concurrency protection, the final balance is negative or one debit is lost!
* **The Solution (Optimistic Concurrency):**
  * In `Wallets`, we have:
    ```csharp
    public byte[] RowVersion { get; protected set; } = [];
    ```
  * In `WalletConfiguration.cs`:
    ```csharp
    builder.Property(w => w.RowVersion).IsRowVersion();
    ```
  * SQL Server assigns a new 8-byte binary timestamp to `RowVersion` every time a row is updated.
  * When EF Core issues the SQL `UPDATE` statement, it includes:
    ```sql
    UPDATE Wallets SET Balance = 200 WHERE Id = @Id AND RowVersion = @OriginalRowVersion;
    ```
  * If another thread updated the row first, `RowVersion` has changed. The `UPDATE` matches 0 rows!
  * EF Core throws `DbUpdateConcurrencyException`.
  * Our `UnitOfWork` catches this and safely rejects the second request with HTTP 409 Conflict, preserving financial integrity!

### 2.10 Strict Double-Entry Ledger & ACID Transactions
* **ACID Guarantees:**
  * **Atomicity:** All parts of a transfer (Debit sender, Credit receiver, Create Transaction, Create 2x LedgerEntries) succeed together or fail together.
  * In `TransferService`:
    ```csharp
    await _unitOfWork.ExecuteInTransactionAsync(async () => { ... });
    ```
  * If a network failure occurs while inserting the second ledger row, `transaction.RollbackAsync()` is invoked. The sender's debit is rolled back, preventing phantom loss of funds!
* **Double-Entry Principle:** Money cannot be created or destroyed. In FinPay:
  * Sender Debit: PKR 5,000 deducted -> Ledger records `Debit: PKR 5,000`.
  * Receiver Credit: PKR 5,000 added -> Ledger records `Credit: PKR 5,000`.
  * Sum of Debits == Sum of Credits.

### 2.11 Read-Only Optimization with AsNoTracking()
* **Concept:** By default, EF Core tracks all entities loaded into memory to detect changes for `SaveChanges()`.
* **In this Project:**
  * In `TransactionRepository.GetWalletHistoryAsync` and `CountWalletHistoryAsync`:
    ```csharp
    _context.Transactions.AsNoTracking()...
    ```
* **Why?** Read-only statement queries don't modify data. `AsNoTracking()` bypasses EF Core change tracker snapshots, saving memory and boosting query speed by up to 300%.

---

## 3. ASP.NET Core Web API & Clean Architecture

### 3.1 Clean Architecture (4-Layer Inward Dependency)
```
FinPay.API (HTTP / Swagger / Middleware)
   │
   ▼ references
FinPay.Infrastructure (EF Core / SQL Server / Migrations)
   │
   ▼ references
FinPay.Application (Use cases / Orchestration / DTOs)
   │
   ▼ references
FinPay.Domain (Core entities / Invariants / Business rules)
```
* **The Dependency Rule:** Dependencies must always point inward.
* `FinPay.Domain` knows **nothing** about SQL Server, EF Core, or HTTP Controllers.
* Why is this powerful? If the database technology is swapped tomorrow (e.g. from SQL Server to PostgreSQL), the core domain entities and business rules in `FinPay.Domain` remain 100% untouched.

### 3.2 Dependency Injection (DI) Lifetimes
Registered in `Program.cs` via ASP.NET Core's built-in IoC container:
1. **`AddScoped`:** Created once per HTTP request. Disposed when request ends.
   * `AppDbContext`: Ensures all repositories within one HTTP request share the same database connection and transaction boundary.
   * `ITransferService`, `IWalletService`, `IAuthService`.
   * `IUserRepository`, `IWalletRepository`, `ITransactionRepository`, `IUnitOfWork`.
2. **`AddSingleton`:** Created once when the application starts, shared across all requests.
   * `IPasswordHasher`: Pure cryptographic calculation class with no mutable state.
3. **`AddTransient`:** Created every time it is requested.

### 3.3 The Middleware Pipeline
In `Program.cs`, middleware order is strictly enforced:
```
Request In
   │
   ▼ 1. ExceptionHandlingMiddleware (Catches unhandled exceptions)
   │
   ▼ 2. UseRouting (Matches URL to endpoint)
   │
   ▼ 3. UseCors (Validates allowed browser origins)
   │
   ▼ 4. UseAuthentication (Validates JWT token signature & claims)
   │
   ▼ 5. UseAuthorization (Validates user roles: Customer vs Admin)
   │
   ▼ 6. MapControllers (Executes controller action)
```
* If authentication ran *before* routing, or exception handling ran *after* controllers, errors would escape uncaught or routes would fail security checks.

### 3.4 Global Exception Handling Middleware
* Located in [`ExceptionHandlingMiddleware.cs`](file:///c:/Users/miell/OneDrive/Desktop/FinPay%20—%20Digital%20Wallet/backend/src/FinPay.API/Middleware/ExceptionHandlingMiddleware.cs).
* Instead of messy `try-catch` blocks inside every controller action, this middleware wraps the entire HTTP pipeline:
  ```csharp
  try {
      await _next(context);
  } catch (Exception ex) {
      await HandleExceptionAsync(context, ex);
  }
  ```
* Maps exceptions to clean JSON:
  * `InsufficientFundsException` -> `400 Bad Request` (`INSUFFICIENT_FUNDS`)
  * `WalletFrozenException` -> `400 Bad Request` (`WALLET_FROZEN`)
  * `SelfTransferException` -> `400 Bad Request` (`SELF_TRANSFER_FORBIDDEN`)
  * `DailyLimitExceededException` -> `400 Bad Request` (`DAILY_LIMIT_EXCEEDED`)
  * `UnauthorizedException` -> `401 Unauthorized` (`UNAUTHORIZED`)
  * `ConcurrencyException` -> `409 Conflict` (`CONCURRENCY_CONFLICT`)
  * Unhandled crashes -> `500 Internal Server Error` (without leaking internal stack traces)

### 3.5 JWT Bearer Authentication & Role-Based Authorization
* **JWT (JSON Web Token) Structure:** `Header.Payload.Signature`.
* The server cryptographically signs the token with `HmacSha256` using `Jwt:SecretKey`.
* When a user logs in, the server embeds claims:
  * `ClaimTypes.NameIdentifier`: User's Guid
  * `ClaimTypes.Role`: `Customer` or `Admin`
  * `wallet_number`: User's 10-digit wallet number
* **Role-Based Authorization:**
  ```csharp
  // Located in backend/src/FinPay.API/Controllers/AdminController.cs
  [Authorize(Roles = "Admin")]
  public class AdminController : ControllerBase
  ```
  If a standard `Customer` user attempts to call this endpoint, ASP.NET Core returns `403 Forbidden` before the controller action even starts!

### 3.6 Thin Controllers vs. Service Layer
* **Best Practice:** Controllers should only be responsible for HTTP concerns: receiving the request body, calling the service, and returning the HTTP status code (`Ok()`, `Created()`, `BadRequest()`).
* In `TransfersController`:
  ```csharp
  [HttpPost("p2p")]
  public async Task<IActionResult> TransferP2P([FromBody] TransferRequestDto request, CancellationToken cancellationToken)
  {
      var senderUserId = _currentUserService.GetRequiredUserId();
      var result = await _transferService.TransferFundsAsync(senderUserId, request, cancellationToken);
      return Ok(ApiResponse<TransferResponseDto>.Ok(result, "P2P transfer completed successfully."));
  }
  ```
  Notice: No database queries, no fee math, no balance checks inside the controller. Everything is delegated to `TransferService`!

### 3.7 The DTO (Data Transfer Object) Pattern
* **Why never return Domain Entities from API Controllers?**
  1. **Security / Information Leakage:** Returning `User` directly would expose `PasswordHash` over the wire!
  2. **Circular References:** `User` references `Wallet`, which references `Transactions`, which reference `User`. System.Text.Json would throw a circular reference exception.
  3. **Over-Posting / Mass Assignment:** Accepting entities in POST requests allows malicious users to send properties they shouldn't change (e.g. sending `Balance = 999999` in a profile update).
  4. DTOs decouple the external API contract from internal database structure.

### 3.8 OpenAPI / Swagger Integration
* Swashbuckle generates interactive OpenAPI documentation at `/swagger`.
* Configured with an `OpenApiSecurityScheme` for JWT Bearer tokens, allowing developers to paste their JWT token directly into the browser to test authorized endpoints.

---

## 4. Unit & Relational Integration Testing

### 4.1 Unit Testing with xUnit & Moq
* Located in `backend/tests/FinPay.Tests/Unit/`.
* **Mocking Dependencies:** `TransferService` depends on `IWalletRepository`, `ITransactionRepository`, and `IUnitOfWork`. In unit tests, we use `Moq` to simulate repository responses without needing a database.
* Tests verify business outcomes, exceptions, and side effects rather than just method invocations.

### 4.2 Relational Integration Testing with SQL Server LocalDB
* Located in `backend/tests/FinPay.Tests/Integration/`.
* Uses `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>`.
* Executes tests against real SQL Server LocalDB (`(localdb)\MSSQLLocalDB`).
* **Why Real SQL Server instead of InMemory?**
  * EF Core's InMemory provider does **not** enforce foreign key constraints, does **not** evaluate SQL transaction rollbacks, and does **not** evaluate SQL Server `rowversion` concurrency tokens.
  * Using real SQL Server guarantees that ACID transactions, concurrency exceptions, and ledger parity work in real relational conditions.

---

## 5. Interview Questions & How to Answer Them

### Q1: "How do you prevent negative balances and double spending in concurrent transfers?"
> *"I use a defense-in-depth strategy: First, domain encapsulation in `WalletBase.Debit()` prevents balances from falling below zero at the business logic level. Second, to protect against concurrent race conditions where two simultaneous requests read the same balance, I configured optimistic concurrency on the `Wallets` table using SQL Server's `rowversion` column with EF Core's `IsRowVersion()`. If two transactions attempt to update the same wallet concurrently, one transaction commits and increments the row version, causing the second transaction's `UPDATE` statement to match 0 rows. EF Core raises a `DbUpdateConcurrencyException`, which is caught and translated to an HTTP 409 Conflict response. Finally, all operations run inside an atomic `IDbContextTransaction`, ensuring that debits, credits, and ledger entries commit or roll back together."*

### Q2: "Why did you implement a double-entry ledger instead of just updating the wallet balance?"
> *"In financial systems, updating a balance alone leaves no verifiable audit trail. If a customer questions why their balance decreased by PKR 5,000, a single balance column cannot prove what happened. By implementing an immutable `LedgerEntries` table, every balance mutation produces a permanent audit record referencing the transaction, the amount, the direction (Debit/Credit), and the balance after the operation. In any successful P2P transfer, exactly two balanced ledger rows are written: one Debit and one Credit. This ensures zero data loss and compliance with standard accounting practices."*

### Q3: "Why did you choose Clean Architecture for this Web API?"
> *"Clean Architecture enforces the Dependency Inversion Principle. The core business rules and entities reside in `FinPay.Domain` with zero dependencies on frameworks, databases, or external libraries. The `FinPay.Application` layer coordinates use cases and defines repository abstractions. Persistence details are implemented in `FinPay.Infrastructure`, and HTTP concerns live in `FinPay.API`. This separation makes business logic easy to test in isolation with unit tests, prevents leaky abstractions, and allows swapping external infrastructure without modifying domain logic."*

### Q4: "How did you implement different transfer limits and fees for Basic and Merchant wallets?"
> *"I used object-oriented polymorphism with EF Core Table-Per-Hierarchy (TPH) inheritance mapping. `WalletBase` is an abstract class defining abstract methods `CalculateFee(decimal amount)` and `DailyTransferLimit`. `BasicWallet` overrides them to return a PKR 50,000 daily limit and PKR 0 fee. `MerchantWallet` overrides them to return a PKR 1,000,000 limit and a 0.5% incoming service fee. EF Core maps both classes into a single `Wallets` table with `WalletType` as the discriminator column, giving us clean polymorphic runtime dispatch in C# without complex database joins."*

### Q5: "How do you ensure a user cannot transfer money from someone else's wallet?"
> *"The API never accepts a sender wallet ID from the client payload. The sender's identity is extracted exclusively on the server from cryptographically signed claims inside the authenticated user's JWT Bearer token using `ClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)`. The client only supplies the recipient identifier and the transfer amount."*
