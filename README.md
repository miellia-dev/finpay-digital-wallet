# FinPay Core Engine & Digital Banking Portal

> **High-Integrity Financial Backend Engine & Modern Digital Banking Frontend**  
> Backend: C#, ASP.NET Core Web API (.NET 10.0 LTS), Entity Framework Core 10, SQL Server, Clean Architecture  
> Frontend: React 19, TypeScript, Vite, Tailwind CSS v4, Lucide Icons

---

## 1. System Architecture & Flow

FinPay Core Engine is a high-integrity financial transaction engine engineered to power digital wallets and real-time peer-to-peer (P2P) fund transfers (similar in concept to SadaPay, NayaPay, or Wise).

The repository is structured into two decoupled directories:
* `backend/`: Clean Architecture .NET 10 Web API, domain entities, double-entry ledger, and SQL Server persistence.
* `frontend/`: Responsive React + TypeScript single-page application demonstrating user journeys and administrative workflows.

```
┌──────────────────────────────────────────────────────────────┐
│                  FRONTEND (React + TypeScript)               │
│   Dashboard | Wallet Details | P2P Transfers | Statements    │
└──────────────────────────────┬───────────────────────────────┘
                               │ HTTPS / JSON (Bearer JWT)
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                    FinPay.API (ASP.NET Core)                 │
│      Controllers | JWT Authentication | Global Middleware     │
└──────────────┬───────────────────────────────┬───────────────┘
               │                               │
               ▼                               ▼
┌──────────────────────────────┐ ┌─────────────────────────────┐
│      FinPay.Application      │ │    FinPay.Infrastructure    │
│  Use Cases, Limit Validation,│ │   EF Core 10, Repositories, │
│  Fee Engine, Orchestration   │ │   UnitOfWork, Transactions  │
└──────────────┬───────────────┘ └─────────────┬───────────────┘
               │                               │
               └───────────────┬───────────────┘
                               ▼
┌──────────────────────────────────────────────────────────────┐
│                   FinPay.Domain (Core Model)                 │
│  WalletBase, BasicWallet, MerchantWallet, Double-Entry Ledger │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│             Microsoft SQL Server / LocalDB                   │
│   Users | Wallets (RowVersion) | Transactions | LedgerEntries │
└──────────────────────────────────────────────────────────────┘
```

---

## 2. Repository Layout

```text
FinPay/
├── backend/
│   ├── FinPay.sln                          # Master solution (.NET 10 LTS)
│   ├── Directory.Build.props               # Solution-wide build configurations
│   ├── dotnet-tools.json                   # Local dotnet-ef CLI tool manifest
│   │
│   ├── src/
│   │   ├── FinPay.Domain/                  # Enterprise Domain Model (Zero external dependencies)
│   │   │   ├── Common/BaseEntity.cs        # Entity base with Guid identity
│   │   │   ├── Entities/                   # WalletBase, BasicWallet, MerchantWallet, User, Transaction, LedgerEntry
│   │   │   ├── Enums/                      # UserRole, WalletType, WalletStatus, TransactionStatus, LedgerEntryType
│   │   │   └── Exceptions/                 # DomainValidationException, InsufficientFundsException, WalletFrozenException
│   │   ├── FinPay.Application/             # Use Cases & Application Orchestration
│   │   │   ├── Common/                     # ApiResponse<T>, PagedResult<T>
│   │   │   ├── DTOs/                       # Immutable C# records for requests & responses
│   │   │   ├── Interfaces/                 # IUnitOfWork, IWalletRepository, ITransferService, etc.
│   │   │   ├── Services/                   # TransferService, WalletService, AuthService
│   │   │   └── Exceptions/                 # SelfTransferException, DailyLimitExceededException, etc.
│   │   ├── FinPay.Infrastructure/          # Persistence & SQL Server Details
│   │   │   ├── Persistence/                # AppDbContext, TPH mapping, UnitOfWork, Migrations
│   │   │   ├── Repositories/               # UserRepository, WalletRepository, TransactionRepository (LINQ)
│   │   │   └── Services/                   # PasswordHasher (PBKDF2 100k iter), TokenService (JWT)
│   │   └── FinPay.API/                     # Presentation & Web API
│   │       ├── Controllers/                # Auth, Wallets, Transfers, Transactions, Admin
│   │       ├── Middleware/                 # ExceptionHandlingMiddleware (HTTP status mapping)
│   │       ├── Services/                   # CurrentUserService (ClaimsPrincipal extraction)
│   │       └── Program.cs                  # DI, CORS policy, JWT Bearer auth, Swagger/OpenAPI
│   │
│   └── tests/
│       └── FinPay.Tests/                   # 47 Unit & Integration Tests (100% Pass Rate)
│           ├── Unit/                       # Invariant, encapsulation, and Moq service tests
│           └── Integration/                # Real LocalDB transactions, rollbacks, and ownership tests
│
├── frontend/                               # React + TypeScript + Vite Single-Page Application
│   ├── .env.example                        # Environment variables template
│   ├── package.json                        # Dependencies (React 19, Lucide, Tailwind CSS v4)
│   ├── vite.config.ts                      # Vite configuration with Tailwind integration
│   └── src/
│       ├── types/                          # TypeScript API, Auth, Wallet, Transfer, Transaction models
│       ├── services/                       # Centralized apiRequest client, authService, walletService, transferService
│       ├── context/                        # AuthContext (JWT session state & role guards)
│       ├── components/
│       │   ├── Common/                     # LoadingSpinner, StatusBadge, ErrorMessage, ConfirmModal
│       │   ├── Layout/                     # Sidebar, Header, AppLayout
│       │   └── Guards/                     # ProtectedRoute, AdminRoute
│       └── pages/                          # Login, Register, Dashboard, Wallet, SendMoney, Transactions, Admin
│
└── README.md                               # Project documentation & operational runbook
```

---

## 3. Backend Setup & Execution Guide

### Prerequisites
* **.NET 10.0 SDK** (verified with `dotnet --info`)
* **Microsoft SQL Server LocalDB** (`sqllocaldb`) or SQL Server Express / Standard

### Step 1: Start SQL Server LocalDB
```powershell
sqllocaldb start MSSQLLocalDB
```

### Step 2: Database Migrations
Navigate to the root and apply the EF Core Code-First migration to SQL Server:
```powershell
# Restore local dotnet-ef tool
dotnet tool restore

# Apply migration to SQL Server LocalDB
dotnet tool run dotnet-ef database update --project backend/src/FinPay.Infrastructure --startup-project backend/src/FinPay.API
```

### Step 3: Run the Backend API
```powershell
dotnet run --project backend/src/FinPay.API --urls "http://localhost:5088"
```
The API is now active at `http://localhost:5088`.
* **Swagger UI Documentation:** `http://localhost:5088/swagger`

### Step 4: Run the Backend Automated Test Suite
Execute all 47 unit and relational integration tests:
```powershell
dotnet test backend/FinPay.sln
# Output: Passed! - Failed: 0, Passed: 47, Skipped: 0, Total: 47
```

---

## 4. Frontend Setup & Execution Guide

### Prerequisites
* **Node.js** v20+ (tested with Node `v26.7.0` and npm `11.19.0`)

### Step 1: Install Dependencies
```powershell
cd frontend
npm install
```

### Step 2: Configure Environment
Copy `.env.example` to `.env`:
```powershell
cp .env.example .env
```
Ensure `VITE_API_BASE_URL` points to the running backend:
```env
VITE_API_BASE_URL=http://localhost:5088
```

### Step 3: Start Frontend Development Server
```powershell
npm run dev
```
Open your browser and navigate to `http://localhost:5173`.

### Step 4: Build for Production
```powershell
npm run build
```

---

## 5. Required Pages & User Experience

| Page | Route | Access | Functional Capabilities |
| :--- | :--- | :--- | :--- |
| **Login** | `/login` | Public | Authenticates credentials with backend; stores JWT session. |
| **Registration** | `/register` | Public | Generates a 10-digit wallet number; provisions a Basic Wallet with 0 balance. |
| **Dashboard** | `/` | Authorized | Displays authoritative wallet balance, wallet number, daily transfer limit progress bar, and recent ledger statements. Includes simulated deposit top-up modal. |
| **Wallet Details** | `/wallet` | Authorized | Displays account tier (Basic vs. Merchant), non-negative balance invariant guarantees, and polymorphic fee schedule. |
| **Send Money** | `/send` | Authorized | P2P transfer submission with recipient resolution (wallet number or phone), daily limit check, and two-step confirmation modal. |
| **Transactions** | `/transactions` | Authorized | Paginated statements with server-side filters (type, date range, page size) strictly scoped to user's wallet. |
| **Admin Console** | `/admin` | Admin Only | Allows administrators to freeze or unfreeze accounts with audit reason tracking. Blocked wallets immediately reject transfers. |

---

## 6. Financial Safety & Core Business Invariants

1. **Non-Negative Balance Invariant:** Enforced inside `WalletBase.Debit(amount)`. Balances can never fall below zero under any condition.
2. **Double-Entry Ledger Parity:** Every successful P2P transfer commits exactly two immutable ledger records:
   * **Debit Entry:** `Amount = TransferAmount`, `BalanceAfter = SenderNewBalance`.
   * **Credit Entry:** `Amount = NetCredited`, `BalanceAfter = ReceiverNewBalance`.
3. **Transaction Rollback:** If a database error or ledger append failure occurs, the entire database transaction rolls back, leaving balances untouched.
4. **Optimistic Concurrency Protection:** SQL Server `rowversion` column on `Wallets` detects simultaneous overlapping updates and throws `DbUpdateConcurrencyException`, translated to HTTP 409 Conflict.
5. **Self-Transfer Prevention:** Server validates that sender wallet and receiver wallet are not identical.
6. **Tier Limits:**
   * **Basic Wallet:** Maximum PKR 50,000 outgoing transfers per 24h UTC window. PKR 0 P2P fee.
   * **Merchant Wallet:** Maximum PKR 1,000,000 outgoing transfers per 24h UTC window. 0.5% incoming service fee deducted from credit.
7. **Client Non-Authority:** The frontend never determines or calculates authoritative balances; all state is derived exclusively from server responses.

---

## 7. Known Limitations & Production Roadmap

* **Simulated Deposit Gateway:** The deposit endpoint `/api/wallets/deposit` provides simulated development funds. In production, this would be backed by Raast, 1Link, or card acquirer webhooks.
* **Client Idempotency Keys:** Current transaction reference IDs are generated server-side. Introducing an `Idempotency-Key` HTTP header allows client network retry deduplication.
* **Refresh Token Rotation:** Current session uses standard 60-minute JWT Bearer tokens. Production deployment would benefit from sliding HttpOnly refresh token rotation.
