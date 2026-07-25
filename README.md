# DigiPOSE - Enterprise B2B Retail & POS Management Platform
**Official Architecture Overview & Deployment Manual (v1.0.0)**

DigiPOSE is a robust, high-performance Point of Sale (POS), Enterprise Resource Planning (ERP), and Online E-Commerce system engineered for scalable retail operations and B2B cloud SaaS distribution. The platform integrates real-time in-store transaction terminal capabilities with an API-driven online storefront and an administrative CMS backoffice.

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
 ├── Master Data Management (26 Tables)       ├── POS Operations & Shifts
 ├── Financial Analytics & SLA Reports        ├── Storefront Catalog & Checkout
 └── Role-Based Access Control (RBAC)         └── Asynchronous SMTP E-Invoice Queue
         │                                               │
         └───────────────────────┬───────────────────────┘
                                 │
                   (Entity Framework Core 10)
                                 ▼
                  [ SQL SERVER DATABASE ENGINE ]
```

### Core Architectural Features:
* **Dual Sales Architecture**: Simultaneous operation of real-time cash drawer POS terminals and online e-commerce portals without accounting ledger contamination. Shopping carts operate inside temporary session buffers and only register as production invoices upon completed checkout.
* **High-Throughput Database Engine**: Implements EF Core `DbContextPooling` to reduce Garbage Collection overhead and maximize transactional concurrency under peak retail load.
* **Automated SEO & SSR Metadata**: Next.js App Router integrates dynamic server-side rendering with metadata injection for product catalogs and SaaS subscription assets.
* **Audit & Ledger Integrity**: Financial ledgers store immutable snapshot records of customer identities, prices, and taxes at checkout, ensuring complete accounting auditing integrity.

---

## 📁 2. Repository Structure

This codebase follows a modular corporate monorepo organization:

```
digipose/
├── backend/                  # Backend applications (.NET SDK 10.0)
│   └── DigiPOSE/             # ASP.NET Core MVC & RESTful Web API project
│       ├── Areas/            # Administrator Backoffice CMS MVC views and controllers
│       ├── Controllers/      # MVC Web Controllers & API REST Endpoints (Controllers/Api/)
│       ├── Models/           # EF Core Entities, Database Context, and DTO Schemas
│       ├── Services/         # Business logic & asynchronous MailKit service integration
│       ├── Views/            # Razor Server-Side Web & POS layout templates
│       └── wwwroot/          # Hosted stylesheet frameworks and uploaded product media
├── frontend/                 # Client web application (Node 20+)
│   ├── app/                  # Next.js 15 App Router pages, layout, and styling
│   ├── components/           # Reusable functional user interface and structure components
│   ├── services/             # API client network fetchers and Axios endpoints
│   ├── store/                # Zustand local client state management buffers
│   └── types/                # Strict TypeScript interface declarations
├── docs/                     # System deployment architecture & functional domain specifications
└── asset/                    # System branding, documentation media, and static assets
```

---

## 💻 3. Technology Stack & Prerequisites

### Technology Stack
* **Backend Runtime**: .NET 10.0 SDK, ASP.NET Core MVC, ASP.NET Core Web API.
* **Database & ORM**: Microsoft SQL Server 2022+, Entity Framework Core 10, System.Linq.Dynamic.Core.
* **Frontend Runtime**: Node.js v20+, Next.js 15, React 19, TypeScript, Tailwind CSS v4, PostCSS.
* **State & Network**: Zustand, TanStack React Query, Axios.
* **Security & Utility**: BCrypt.Net-Next (Hash Encryption), MailKit (SMTP Electronic Receipt Delivery), Stateless JWT Bearer & Secure HTTP-Only Cookie Authentication.

### Prerequisites & Required Tooling
Ensure your local host has the following tooling pre-installed before initiating builds:
* [.NET SDK 10.0+](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Node.js (v20.x or higher) & npm](https://nodejs.org/)
* [Microsoft SQL Server (2019/2022) or SQL Server Developer/Express](https://www.microsoft.com/en-us/sql-server)
* [Git Version Control](https://git-scm.com/)

---

## 🚀 4. Build, Installation & Execution Guide

Follow this systematic guide to configure, build, and run the enterprise system locally.

### Step 1: Clone Repository & Configure Database Connection
1. Clone the master project repository:
   ```bash
   git clone <repository-url> digipose
   cd digipose
   ```
2. Navigate to the backend directory and open `appsettings.json`:
   ```bash
   cd backend/DigiPOSE
   ```
3. Update the `DefaultConnection` string to point to your SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=DigiPOSE_DB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

### Step 2: Install Backend Packages & Apply Database Schema
1. Restore standard NuGet package libraries and dependencies:
   ```powershell
   dotnet restore
   ```
2. Verify project compilation without structural errors:
   ```powershell
   dotnet build --nologo -v q
   ```
3. Execute EF Core database migration to generate standard tables and initial seeds:
   ```powershell
   dotnet ef database update
   ```
   *(Note: If EF CLI tooling is absent, install it via: `dotnet tool install --global dotnet-ef`)*

### Step 3: Start ASP.NET Core Backend & REST API Server
Launch the backend application dev server:
```powershell
dotnet run
```
The server will initialize on port `5128` by default. You can verify network availability via:
* **Administrator Backoffice CMS**: `http://localhost:5128/Administrator`
* **Direct MVC Storefront Portal**: `http://localhost:5128/Storefront`
* **RESTful Web API Gateway**: `http://localhost:5128/api/v1/Storefront/user-identity`

---

### Step 4: Install & Launch Frontend Next.js Web Client
Open a secondary external terminal window and navigate to the frontend directory:
1. Navigate to the client directory:
   ```powershell
   cd d:\Study\ASP_Web_Technology\Project\digipose\frontend
   ```
2. Install Node.js libraries and PostCSS / Tailwind CSS engine packages:
   ```powershell
   npm install
   ```
3. Start the Next.js development server:
   ```powershell
   npm run dev
   ```
The frontend application will compile and initialize on port `3000`:
* **Online E-Commerce & SaaS Storefront**: `http://localhost:3000/`
* **In-Store Cashier POS Terminal**: `http://localhost:3000/pos`
* **Shopping Cart & Checkout Buffer**: `http://localhost:3000/cart`

---

## 🏗 5. Production Build & Deployment Guide

For enterprise staging and production environments, optimize binaries and static assets as follows:

### Backend Production Publishing
Compile optimized release assemblies and required runtime libraries:
```powershell
cd backend/DigiPOSE
dotnet publish -c Release -o ./publish
```
Configure IIS, Kestrel, or Linux Docker containers to execute `DigiPOSE.dll` directly under strict Reverse Proxy and HTTPS redirection policies.

### Frontend Static & SSR Bundle Generation
Generate highly optimized production client bundles and statically optimized metadata pages:
```powershell
cd frontend
npm run build
npm run start --port 3000
```

---

## 🔐 6. Enterprise Security Guardrails
* **Secret Isolation**: Never commit active API keys, JWT secrets, or production database credentials into source control. Always maintain `.env` and `appsettings.Production.json` file entries inside `.gitignore`.
* **Tenant Isolation**: Backend RESTful endpoints strictly enforce JWT token decoding and user identity validation to prevent IDOR (Insecure Direct Object Reference) anomalies.
* **ACID Transaction Security**: Checkout executions (`StorefrontController.cs`) operate within explicit serializable database transactions (`BeginTransactionAsync`), guaranteeing rollback protection during system interruptions or inventory concurrency locking.
