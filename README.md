# AEM Enersol — Technical Assessment

A **.NET 8 Web API** that syncs Platform and Well data from the AEM remote REST API into a **SQL Server** database using **Entity Framework Core (Code First)**.

---

## Tech Stack

| | |
|---|---|
| Framework | ASP.NET Core Web API (.NET 8) |
| ORM | Entity Framework Core 8 (Code First) |
| Database | SQL Server (SQL Express) |
| API Documentation | Swagger / OpenAPI |

---

## Project Structure

```
AEMTest.Api/
├── Controllers/
│   └── SyncController.cs          # REST endpoints to trigger sync & read data
├── Data/
│   ├── AppDbContext.cs            # EF Core DbContext
│   └── Migrations/                # Code First migration files
├── Models/
│   ├── Entities/
│   │   ├── Platform.cs            # Platforms table entity
│   │   └── Well.cs                # Wells table entity (FK → Platform)
│   └── Dtos/
│       ├── PlatformDto.cs         # Actual API response shape
│       ├── WellDto.cs
│       ├── PlatformDummyDto.cs    # Dummy API (different JSON keys)
│       ├── WellDummyDto.cs
│       ├── LoginRequest.cs
│       ├── LoginResponse.cs
│       └── SyncResult.cs
├── Services/
│   ├── RemoteApiService.cs        # Login + HTTP fetch from remote API
│   └── SyncService.cs             # Upsert logic (insert or update by ID)
├── appsettings.json               # Connection string + API credentials
└── Program.cs                     # DI registration, Swagger, auto-migration
```

---

## How to Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or SQL Express)

### 1. Configure the database connection

Edit `AEMTest.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=aem_assessment;Trusted_Connection=True;TrustServerCertificate=True"
}
```

> The database will be **created automatically** on first run via `MigrateAsync()`. No manual SQL scripts needed.

### 2. Run the application

```bash
cd AEMTest.Api
dotnet run
```

### 3. Open Swagger UI

Navigate to **http://localhost:5050** (or the port shown in the terminal).

---

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/Sync/run` | Login + fetch **Actual** data + upsert to DB |
| `POST` | `/api/Sync/run-dummy` | Login + fetch **Dummy** data + upsert to DB |
| `GET`  | `/api/Sync/platforms` | List all Platforms in the database |
| `GET`  | `/api/Sync/platforms/{id}/wells` | List Wells for a specific Platform |
| `GET`  | `/api/Sync/wells` | List all Wells in the database |

---

## Requirements Fulfilled

- ✅ **Login** to the Web API with bearer token authentication
- ✅ **Fetch** data from `GetPlatformWellActual` using the bearer token
- ✅ **Store** Platform data in `Platforms` table, Well data in `Wells` table
- ✅ **Upsert** — insert if ID doesn't exist, update if it does
- ✅ **Resilient deserialization** — handles missing/different keys (`GetPlatformWellDummy`) without breaking. Uses nullable DTOs + `System.Text.Json`'s default behavior of ignoring unknown keys
- ✅ **Code First** approach with Entity Framework Core migrations
- ✅ **SQL Server** LocalDB / SQL Express

---

## Key Design Decisions

### Resilient JSON Deserialization
All DTO properties are **nullable** with explicit `[JsonPropertyName]` attributes.  
`System.Text.Json` silently ignores unknown/extra keys by default — so if the API adds new fields or removes existing ones, no exception is thrown.

The Dummy API uses `lastUpdate` instead of `createdAt`/`updatedAt`. Both are mapped in the Dummy DTOs, with `lastUpdate` used as a fallback.

### Upsert Logic
```csharp
var existing = await _dbContext.Platforms.FindAsync(id);
if (existing is null)
    // INSERT
else
    // UPDATE — null-safe: only overwrites fields that are non-null in the API response
```

### Auto-Migration on Startup
```csharp
await db.Database.MigrateAsync(); // in Program.cs
```
No manual `dotnet ef database update` needed — tables are created automatically.

## Remote API Setup

Configure the Remote API credentials locally using **.NET User Secrets** so they are not committed to source control.

Run the following commands in the directory of `AEMTest.Api/`:

```bash
dotnet user-secrets init
dotnet user-secrets set "RemoteApi:Username" "user@aemenersol.com"
dotnet user-secrets set "RemoteApi:Password" "Test@123"
```

The app will read these configuration values automatically from your machine's secret store during development.

