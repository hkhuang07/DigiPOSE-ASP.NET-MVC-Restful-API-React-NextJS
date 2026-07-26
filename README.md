# DigiPOSE - Enterprise B2B Retail & POS Management Platform
**Official Architecture Overview & Feature Specification Manual (v1.0.0)**

DigiPOSE is a robust, high-performance Point of Sale (POS), Enterprise Resource Planning (ERP), and Online E-Commerce platform engineered for scalable retail operations and B2B cloud SaaS distribution. The platform integrates real-time in-store transaction terminal capabilities with an API-driven online storefront and an administrative CMS backoffice.

---

## 🏛 1. System Architecture Overview

DigiPOSE adopts a decoupled domain-driven architecture separating public commerce interfaces from administrative core ledgers:

```
                  [ CLIENT WEB & POS TERMINALS ]
               (Next.js 15 / React 19 / TypeScript / Tailwind)
               ├── E-Commerce Storefront ---> http://localhost:3000/
               ├── POS Cashier Terminal  ---> http://localhost:3000/pos
               └── Shopping Cart Bridge  ---> http://localhost:3000/cart
                                 │
                   (RESTful JSON API / JWT Bearer)
                                 │
                                 ▼
               [ ASP.NET CORE MVC & API GATEWAY ]
                     (http://localhost:5128/)
         ┌───────────────────────┴───────────────────────┐
         ▼                                               ▼
[ ADMINISTRATOR CMS ]                         [ RESTful WEB API MODULES ]
 (ASP.NET Core Razor / Cookie Auth)           (Controllers/Api/ -> Stateless JSON)
 ├── Master Data Management (30 Controllers)  ├── POS Operations & Shifts (PosController)
 ├── Financial Analytics & SLA Reports        ├── Storefront Catalog & Checkout (Storefront)
 └── Role-Based Access Control (RBAC)         └── Real-Time SignalR WebSockets (PosRealtimeHub)
         │                                               │
         └───────────────────────┬───────────────────────┘
                                 │
                [ SERVICES & BACKGROUND WORKERS ]
                ├── IInventoryRAMService (O(1) Memory Manager)
                ├── IVatBalancingEngine (VAT Cent Rounding)
                ├── InventoryWarmupWorker (RAM Pre-loader)
                └── ResilientInvoiceWorker (Async MailKit Queue)
                                 │
                   (Entity Framework Core 10)
                                 ▼
                  [ SQL SERVER DATABASE ENGINE ]
```

---

## ✨ 2. Implemented Features & Core Capabilities

### 🛒 A. High-Speed Cashier POS Terminal (`/pos` & `PosController.cs`)
* **O(1) In-Memory Stock Deduction**: Pre-deducts and validates product stock instantly via `IInventoryRAMService` (<15ms latency) before executing database transactions.
* **Hardware Debounce Guard**: Integrated `IMemoryCache` TTL buffer prevents accidental double-scanning from barcode scanners.
* **VAT Rounding & Balancing Engine (`VatBalancingEngine.cs`)**: Implements an enterprise VAT cent balancing algorithm (`Round(Sum(PreTax) * TaxRate, 2)` vs line-item rounding) that injects tax variance into the primary line item, guaranteeing 100% financial ledger match.
* **Dual-Layer Idempotency Safeguard**: RAM cache checks combined with SQL unique constraint locks eliminate duplicate transaction processing during unstable network connectivity.
* **Cashier Tender & Change Calculation**: Records `TenderedAmount` and automatically computes exact `ChangeAmount` for accurate cash drawer reconciliation.
* **Shift & Counter Management**: Open/close cashier shifts, balance cash drawers, and map terminal counters (`ShiftsController`, `CountersController`).

### 🌐 B. Online E-Commerce Storefront (`/`, `/cart` & `StorefrontController.cs`)
* **Dynamic Product Catalog**: Filtering, pagination, full-text searching, category navigation, and SSR SEO metadata optimization.
* **Session Shopping Cart (`cartStore.ts`)**: Client-side Zustand state buffer supporting real-time stock availability verification.
* **Atomic Order Checkout**: Calculates delivery fees (`ShippingFee`), records recipient information (`ShippingAddress`), saves customer notes (`OrderNotes`), and wraps creation in isolated SQL transactions (`BeginTransactionAsync`).

### 📡 C. Real-Time Telemetry & SignalR Broadcasting (`PosRealtimeHub.cs`)
* **Instant Stock Synchronization**: Broadcasts inventory updates (`OnStockChanged`) across all active POS terminals within <1ms.
* **Low Stock Alerts**: Automatic alert dispatching (`LowStockAlerts <= 5`) to cashiers and store administrators.
* **Live Order Arrival**: Pushes real-time web order notifications (`WEB_ORDER_CREATED`) directly to administrative HUD monitors.

### ⚡ D. Asynchronous Background Engine (`Services/Background/`)
* **`InventoryWarmupWorker`**: Pre-loads active branch inventory levels into high-speed memory on ASP.NET Core startup.
* **`ResilientInvoiceWorker`**: Asynchronous background queue executing electronic invoice generation and MailKit SMTP email dispatching without blocking payment checkout threads.

### 🛡️ E. Enterprise Backoffice CMS (`/Administrator` & `Areas/Administrator/`)
* **30 Master Data Controllers**: Complete administrative CRUD for 26 database entities (Products, Inventories, Categories, Suppliers, Customers, Manufacturers, Units, Tax Types, Payment Methods, etc.).
* **Inventory Restoration & Order Safeguard (`OrdersController.cs`)**: Cancelling or deleting orders automatically restores RAM stock (`RestoreStock`), logs audit vouchers (`InventoryTransactions`), and notifies POS terminals via SignalR.
* **RBAC & Security**: Fine-grained Role-Based Access Control (`Permissions`, `Roles`, `UserRoles`), BCrypt password hashing, and Cookie/JWT authentication.
* **Cyber-Cinematic HUD UI**: High-density military lab aesthetic featuring custom dark mode canvas (`#000000`), neon status indicators (`#00E5FF`, `#00FF66`, `#FFB000`, `#FF3333`), segmented progress bars, and scanline FX.

---

## 📁 3. Repository Structure

```
digipose/
├── backend/                  # Backend applications (.NET SDK 10.0)
│   └── DigiPOSE/             # ASP.NET Core MVC & RESTful Web API project
│       ├── Areas/            # Administrator Backoffice CMS MVC views and controllers (30 Controllers)
│       ├── Controllers/      # API REST Endpoints (Controllers/Api/ -> PosController, StorefrontController)
│       ├── Hubs/             # Real-Time WebSocket Hubs (PosRealtimeHub)
│       ├── Models/           # EF Core Entities, Database Context, and DTO Schemas
│       ├── Services/         # Business logic, RAM inventory manager & VAT balancing engine
│       │   └── Background/   # Hosted services (InventoryWarmupWorker, ResilientInvoiceWorker)
│       ├── Views/            # Razor Server-Side Web & Cyber-HUD layout templates
│       └── wwwroot/          # Hosted stylesheet frameworks and uploaded product media
├── frontend/                 # Client web application (Node 20+)
│   ├── app/                  # Next.js 15 App Router pages (/, /pos, /cart)
│   ├── components/           # Reusable functional UI & Cyber-HUD components (CyberNavbar, CyberSidebar)
│   ├── services/             # API client network fetchers and Axios endpoints
│   ├── store/                # Zustand local client state management (cartStore, authStore)
│   └── types/                # Strict TypeScript interface declarations
├── docs/                     # System deployment architecture & functional domain specifications
├── asset/                    # System branding, documentation media, and static assets
└── demo/                     # Screenshots and UI demo media isolation
```

---

## 💻 4. Technology Stack & Prerequisites

### Technology Stack
* **Backend Runtime**: .NET 10.0 SDK, ASP.NET Core MVC, ASP.NET Core Web API, SignalR.
* **Database & ORM**: Microsoft SQL Server 2022+, Entity Framework Core 10, System.Linq.Dynamic.Core.
* **Frontend Runtime**: Node.js v20+, Next.js 15, React 19, TypeScript, Tailwind CSS v4, PostCSS.
* **State & Network**: Zustand, TanStack React Query, Axios.
* **Security & Utility**: BCrypt.Net-Next (Hash Encryption), MailKit (SMTP Electronic Receipt Delivery), Stateless JWT Bearer & Secure HTTP-Only Cookie Authentication.

### Prerequisites & Required Tooling
* [.NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js (v20.x or higher) & npm](https://nodejs.org/)
* [Microsoft SQL Server (2019/2022) or SQL Server Developer/Express](https://www.microsoft.com/en-us/sql-server)
* [Git Version Control](https://git-scm.com/)

---

## 🚀 5. Build, Installation & Execution Guide

### Step 1: Clone Repository & Configure Database Connection
1. Clone the project repository:
   ```bash
   git clone <repository-url> digipose
   cd digipose
   ```
2. Open `backend/DigiPOSE/appsettings.json` and configure `DefaultConnection`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DigiPOSE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

### Step 2: Install Backend Packages & Apply Database Schema
1. Restore NuGet packages:
   ```powershell
   cd backend/DigiPOSE
   dotnet restore
   ```
2. Compile backend project:
   ```powershell
   dotnet build --nologo -v q
   ```
3. Update EF Core database schema and seed initial data:
   ```powershell
   dotnet ef database update
   ```

### Step 3: Start ASP.NET Core Backend & REST API Server
Launch the backend dev server:
```powershell
dotnet run
```
Default endpoint links:
* **Administrator Backoffice CMS**: `http://localhost:5128/Administrator`
* **POS REST API Gateway**: `http://localhost:5128/api/v1/pos/products`
* **Storefront REST API Gateway**: `http://localhost:5128/api/v1/Storefront/user-identity`

---

### Step 4: Install & Launch Frontend Next.js Web Client
Open a separate terminal window:
1. Navigate to frontend:
   ```powershell
   cd frontend
   ```
2. Install packages:
   ```powershell
   npm install
   ```
3. Launch development client:
   ```powershell
   npm run dev
   ```
Default client links:
* **Online E-Commerce Storefront**: `http://localhost:3000/`
* **In-Store Cashier POS Terminal**: `http://localhost:3000/pos`
* **Shopping Cart & Checkout**: `http://localhost:3000/cart`

---

## 🏗 6. Production Build & Deployment Guide

### Backend Release Publishing
```powershell
cd backend/DigiPOSE
dotnet publish -c Release -o ./publish
```

### Frontend Bundle Production Generation
```powershell
cd frontend
npm run build
npm run start --port 3000
```

---

## 🔐 7. Enterprise Security & Ledger Guardrails
* **Secret Isolation**: All credentials, JWT secrets, and SMTP tokens are excluded via `.gitignore` and configured through environment variables.
* **IDOR Prevention**: API endpoints validate decoded JWT tokens and enforce tenant boundaries.
* **ACID Financial Transactions**: Payment checkouts use `BeginTransactionAsync` with explicit rollback handling to ensure zero data corruption.
