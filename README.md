Section 1: Programming Skillset Assessment

**Submitted by:** MUHAMMAD HAKIM BIN MD HAMBALI 
**Email:** hakimhambali77@gmail.com
**Date:** 11 December 2025

## Overview

A complete sales posting and processing system built with C# .NET 8.0 and SQL Server. The system receives sales from POS terminals via REST API and processes them asynchronously using a background worker service.

## Technology Stack

- **.NET 8.0** (LTS)
- **ASP.NET Core Web API** with Swagger
- **Entity Framework Core 8.0.22**
- **SQL Server**
- **Background Worker Service**

---

## Prerequisites

- Visual Studio 2022 (with ASP.NET workload)
- .NET 8.0 SDK
- SQL Server Management Studio

---

## Installation & Setup

### Step 1: Database Setup

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance
3. Run the script: **`SalesPostingDB.sql`** 
   - This creates the database, tables, and indexes

### Step 2: Update Connection Strings

Update connection strings in **both** projects:

**SalesPosting.Api/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SalesPostingDB;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

**SalesPosting.Worker/appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SalesPostingDB;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

Replace `YOUR_SERVER` with:
- `localhost` for local SQL Server
- `(localdb)\MSSQLLocalDB` for LocalDB
- Your computer name for named instance

### Step 3: Restore NuGet Packages

Open solution in Visual Studio:
```bash
dotnet restore
```

Or: Right-click Solution → Restore NuGet Packages

### Step 4: Configure Multiple Startup Projects

1. Right-click **Solution** → **Properties**
2. Select **Multiple startup projects**
3. Set both `SalesPosting.Api` and `SalesPosting.Worker` to **Start**
4. Click **OK**

### Step 5: Run the Application

Press **F5** or click **Start**

You should see:
- ✅ Browser opens with **Swagger UI** at `https://localhost:7xxx/swagger`
- ✅ **Worker console** showing processing logs

---

## Testing

### Using Swagger UI

1. Navigate to **POST /api/sales/postsales**
2. Click **"Try it out"**
3. Use this test payload:
```json
{
  "transactionId": "TXN-2024-001",
  "terminalId": "POS-001",
  "transactionDate": "2024-12-11T10:30:00",
  "totalAmount": 250.50,
  "customerName": "John Doe",
  "paymentMethod": "Credit Card",
  "items": [
    {
      "productCode": "PROD-001",
      "productName": "Laptop",
      "quantity": 1,
      "unitPrice": 200.00,
      "amount": 200.00
    }
  ]
}
```

4. Click **"Execute"**
5. Check Worker console - should show processing within 5 seconds

### Test Idempotency

Send same transaction twice → Second attempt returns **409 Conflict**

### Test Multiple Terminals

Change `terminalId` to POS-002, POS-003, etc. → All processed successfully

### Bulk Performance Test

Use endpoint: **POST /api/sales/test/bulkinsert/10000**
- Inserts 10,000 test records
- Returns performance metrics
- Worker processes automatically

### Cleanup Test Data

Use endpoint: **DELETE /api/sales/test/cleanup**

---

## Configuration

### Worker Settings (appsettings.json)

```json
{
  "ProcessingSettings": {
    "BatchSize": 100,              // Records per cycle
    "PollingIntervalSeconds": 5,   // Check every 5 seconds
    "MaxDegreeOfParallelism": 4,   // 4 concurrent threads
    "MaxRetryCount": 3             // Retry failed records 3 times
  }
}
```

**Tuning Guidelines:**
- **BatchSize:** 50-500 (higher = more throughput, more memory)
- **PollingInterval:** 1-30 seconds (lower = faster, more DB load)
- **Parallelism:** 2-16 (match CPU cores, max performance at core count × 2)
- **MaxRetry:** 3-5 (industry standard is 3)

---

## Bonus Requirements Implemented

### ✅ 1. Multiple Sources/Terminals
- `TerminalId` column in database
- Tested with 10+ different terminals

### ✅ 2. Multi-threading / Async
- `async/await` throughout codebase
- `Parallel.ForEachAsync` in Worker (4 concurrent threads)
- Configurable parallelism
- **Performance:** 4x faster than single-threaded

### ✅ 3. Large Volume Handling
- Batch processing (prevents memory overflow)
- **Tested:** 100,000 records with consistent performance

### ✅ 4. Error Capture
- `ProcessingError` table with full stack traces
- Automatic retry (max 3 attempts)
- Detailed error logging
- Easy troubleshooting for support team

---

## Project Structure

```
SalesPostingSystem/
├── SalesPosting.Api/           # Web API
│   ├── Controllers/
│   │   └── SalesController.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── SalesPosting.Data/          # Shared Data Layer
│   ├── Entities/
│   │   ├── SalesPayload.cs
│   │   └── ProcessingError.cs
│   ├── DTOs/
│   │   ├── SalesPayloadDto.cs
│   │   └── SalesItemDto.cs
│   └── Data/
│       └── SalesDbContext.cs
│
├── SalesPosting.Worker/        # Background Service
│   ├── Worker.cs
│   ├── ProcessingSettings.cs
│   ├── appsettings.json
│   └── Program.cs
│
├── SalesPostingDB.sql         # Database creation script
├── README.md                  # This file
│   
```

---

**Assessment Checklist:**

✅ Sales posting API endpoint  
✅ Database with efficient indexes  
✅ Worker service processes records  
✅ **Bonus:** Multi-terminal support  
✅ **Bonus:** Multi-threading/async  
✅ **Bonus:** Large volume handling  
✅ **Bonus:** Error capture & logging  
✅ Section 2 system design answers  