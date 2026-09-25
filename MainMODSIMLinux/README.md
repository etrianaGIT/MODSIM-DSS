# MainMODSIMLinux

A cross-platform console application for running MODSIM on Linux via .NET 8,
built from the DotNetCore branch migration.

## Overview

This project was created to enable MODSIM to run on Linux environments 
(including Docker containers and WSL) by porting the core simulation engine 
to .NET 8 and replacing `System.Data.SQLite` with `Microsoft.Data.Sqlite`.

---

## Steps Taken

### 1. Create New Linux Console Project
- **Project Type:** C# Console Application (.NET 8)
- **Template:** Linux Console
- **Output:** Executable targeting `net8.0`
- **Location:** `MainMODSIMLinux\MainMODSIMLinux.csproj`

### 2. Update NetworkUtils References
Migrated from `System.Data.SQLite` to `Microsoft.Data.Sqlite`:
- Updated `ModsimMain\NetworkUtils\NetworkUtils.csproj` package references
- All database operations now use `Microsoft.Data.Sqlite` namespace
- Added manual INSERT/UPDATE/DELETE logic in `SQLiteHelper.cs` (see Code Changes section below)

---

## Development Environment Setup

### Debugging in WSL

To run and debug the application directly in Windows Subsystem for Linux:

#### Prerequisites
1. **Install WSL Debugging Support in Visual Studio:**
   - Open Visual Studio Installer (with elevated access)
   - Click **Modify**
   - Go to **Individual Components**
   - Search for ".NET Debugging with WSL"
   - Check and apply

2. **Install .NET 8 in WSL Distribution:**
   ```bash
   sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0
   ```

#### Configuration
Edit `MainMODSIMLinux\Properties\launchSettings.json` with your distribution name and path to a test MODSIM .xy file in Linux.

```json
{
  "profiles": {
	"WSL": {
	  "commandName": "WSL2",
	  "distributionName": "Ubuntu-22.04",
	  "commandLineArgs": "/path/to/input/file.xy"
	}
  }
}
```

---

## Migration Notes: System.Data.SQLite → Microsoft.Data.Sqlite

### Why the Switch?

**System.Data.SQLite Limitations on Linux:**
- Requires platform-specific native C++ interop assemblies (`SQLite.Interop.dll` on Windows, `.so` on Linux)
- Native binaries must exactly match OS, CPU architecture, and runtime environment (glibc vs musl)
- Historically problematic in containerized/headless Linux environments
- Many Stack Overflow, reddit threads, etc. experiencing similar issues during research and attempted fixes recommend migration to Microsoft.Data.Sqlite. 

**Microsoft.Data.Sqlite Advantages:**
- [Microsoft.Data.Sqlite Comparison](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/compare)
- Lightweight, modern ADO.NET provider built for .NET Core/8
- Built on `SQLitePCLRaw`, which bundles precompiled native binaries for all major platforms
- Works out-of-the-box in Docker containers and headless Linux

### Implications of the Switch: No DbDataAdapter

[Microsoft.Data.Sqlite Comparison](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/compare)

**Limitation:** `Microsoft.Data.Sqlite` intentionally omits `DbDataAdapter` and `DbCommandBuilder`
- These APIs were removed to keep the provider lightweight and aligned with modern .NET design
- No equivalent "built-in" mechanism exists for batch table updates

**Implementation:** Manual row-by-row updates in `SQLiteHelper.UpdateTables()`
- Iterate through `DataTable.Rows` and inspect `DataRowState` (Added, Modified, Deleted)
- Build parameterized SQL INSERT/UPDATE/DELETE commands
- Execute per row within a transaction
- See Potential Future Optimizations section for thoughts on improved performance. 

**Alternative Solutions Considered**:

*Note*: ORM stands for Object-Relational Mapping. It is a programming technique that lets
you connect a relational database to an object-oriented programming language. 
It allows developers to work with database data as objects, rather than writing raw SQL queries.	

1. *Entity Framework Core (Full ORM)*
- Pros: Type-safe queries, automatic change tracking, built-in migrations
- Cons: Requires restructuring the data access layer, learning curve noted.
- Initial verdict: Not worth potential learning curve and refactoring for current project scope; adds significant overhead.

2. *Dapper (Micro-ORM)*
- Pros: Minimal overhead, lightweight
- Cons: Still requires manual SQL strings per operation
- Initial verdict: Only marginally reduced boilerplate and slightly cleaner code; adds external dependency


---

## Code Changes

### SQLiteHelper.cs: InsertRow, UpdateRow, DeleteRow

Because `Microsoft.Data.Sqlite` lacks `DbDataAdapter.Update()` as described above, three new private methods were added to `SQLiteHelper.cs`:

#### InsertRow(DataTable, DataRow)
- Builds parameterized INSERT command from non-primary-key columns
- Binds current row values
- Executes within transaction

#### UpdateRow(DataTable, DataRow)
- Builds parameterized UPDATE command
- SET clause: all non-primary-key columns with current values
- WHERE clause: primary key columns with original values (to find the row)
- Executes within transaction

#### DeleteRow(DataTable, DataRow)
- Builds parameterized DELETE command using primary key
- Deletes row identified by original key values
- Executes within transaction


### Potential Future Optimizations
**Command Reuse:** Pre-build INSERT/UPDATE/DELETE command templates once per table, then reuse by only updating parameter values for each row.

- **Expected improvement:** 2-10x speedup on large tables
- **Reference:** See `SQLiteHelper_Optimization_Reference.md` for detailed implementation
- **Recommendation:** Benchmark against production timeseries data before implementing; implement only if profiling shows this as a bottleneck

---

## Known Issues & Quirks

### WSL Database Locking

**Issue:** Database is locked in WSL after executing code. 
This appears to be a SQLite quirk in WSL, not a bug in the code.
When running on Windows, the output database can be opened immediately,
which indicates the locking behavior is specific to WSL. The output file
can also be copied to Windows and opened without issue.

This is an issue with using SQLite in Windows to open a database
in WSL. Accessing the database from WSL directly (e.g., via Python)
shows that the database is accessible -- example script below.

```python
from pathlib import Path
import sqlite3

output_db = Path("MODSIM_gage-11179000_GagesRenamed_ResBypass_runOUTPUT.sqlite")

with sqlite3.connect(output_db) as connection:
    cursor = connection.cursor()

    tables = [row[0] for row in cursor.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name"
    )]
    print("Tables:", tables)

    if tables:
        table_name = tables[0]
        sample_rows = cursor.execute(f'SELECT * FROM "{table_name}" LIMIT 5').fetchall()
        print(f"\nSample rows from {table_name}:")
        for row in sample_rows:
            print(row)
    else:
        print("No tables found in the database.")

print("Database connection closed.")
```

---