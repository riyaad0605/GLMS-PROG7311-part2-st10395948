# GLMS — Global Logistics Management System
### PROG7311 | Part 2 — Core Prototype & Unit Testing
**Student:** Muhammed Riyaad Kajee | **Student No:** ST10395948

---

## Overview
ASP.NET Core 8 MVC monolith managing freight contracts, service requests, and international billing for TechMove Logistics. Includes role-based authentication, email notifications, live currency conversion, and a full xUnit test suite.

---

## Prerequisites
- Visual Studio 2022 (v17.8+)
- .NET 8 SDK
- SQL Server LocalDB (ships with Visual Studio)
- Internet connection (for live exchange rate API)

---

## Setup Steps

### 1. Clone / Open the Solution
Open `GLMS.sln` in Visual Studio 2022.

### 2. Restore NuGet Packages
Visual Studio restores these automatically on build. If not:
```
Tools → NuGet Package Manager → Restore NuGet Packages
```

### 3. Apply the Database Migration
Open the **Package Manager Console** (`Tools → NuGet Package Manager → Package Manager Console`), ensure the default project is **GLMS.Web**, then run:

```powershell
Update-Database
```

This creates the `GLMS_DB` database on `(localdb)\mssqllocaldb`, applies all tables, and seeds:
- 2 roles (Admin, Manager)
- 2 default users
- 3 clients and 3 contracts

> **Alternative (terminal):** `dotnet ef database update --project GLMS.Web`

### 4. Run the Application
Press **F5** or `Ctrl+F5`. The app opens at `https://localhost:xxxx`.

---

## Default Login Accounts

| Role    | Email                  | Password       | Access                          |
|---------|------------------------|----------------|---------------------------------|
| Admin   | admin@glms.com         | Admin@123!     | Full CRUD on all entities       |
| Manager | manager@glms.com       | Manager@123!   | View + raise service requests   |

---

## Running Unit Tests

### Via Test Explorer (Visual Studio)
1. Build the solution (`Ctrl+Shift+B`)
2. Open `Test → Test Explorer`
3. Click **Run All Tests**

### Via Terminal
```bash
dotnet test GLMS.Tests/GLMS.Tests.csproj
```

### Tests Covered

| Test Class            | Tests | What It Covers                                      |
|-----------------------|-------|-----------------------------------------------------|
| CurrencyServiceTests  | 9     | USD→ZAR math, rounding, negative/zero guards        |
| FileServiceTests      | 9     | PDF allowed, .exe/.docx/.png blocked, size limits   |
| WorkflowTests         | 10    | Contract status eligibility, date logic, defaults   |
| **Total**             | **28**|                                                     |

---

## Email Notifications
Email is **optional** — the app runs normally without SMTP configured.

To enable, edit `appsettings.json`:
```json
"EmailSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your@gmail.com",
    "Password": "your-app-password",
    "FromAddress": "your@gmail.com",
    "EnableSsl": true
}
```

Notifications fire when:
- A contract's **status changes** (Admin edits contract)
- A **new service request** is created

---

## Key Features

| Feature | Details |
|---------|---------|
| Authentication | ASP.NET Core Identity with 2 roles |
| Contract Management | Full CRUD, status lifecycle, PDF upload/download |
| LINQ Search | Filter contracts by date range and status |
| Workflow Validation | Blocks service requests on Expired/OnHold contracts |
| Currency Conversion | Live USD→ZAR via ExchangeRate-API, auto-calculates on form |
| Email Notifications | MailKit SMTP on status change and request creation |
| File Handling | PDF-only upload, server-side storage, downloadable |
| Unit Tests | 28 xUnit tests across 3 test classes |

---

## Project Structure
```
GLMS/
├── GLMS.sln
├── GLMS.Web/
│   ├── Controllers/     — Account, Home, Clients, Contracts, ServiceRequests
│   ├── Data/            — GlmsDbContext (IdentityDbContext)
│   ├── Migrations/      — InitialCreate migration
│   ├── Models/          — Entity models + ViewModels
│   ├── Services/        — CurrencyService, FileService, EmailService
│   └── Views/           — All Razor views
└── GLMS.Tests/
    ├── CurrencyServiceTests.cs
    ├── FileServiceTests.cs
    └── WorkflowTests.cs
```

---

## Architecture Decisions
- **Monolith pattern** — all layers in one ASP.NET Core project 
- **Strategy pattern** — `ICurrencyService` allows swap between live API and fallback rate
- **Observer-ready** — `IEmailService` decouples notification logic from controllers
- **Factory-ready** — `IFileService` abstracts file storage for future cloud migration
