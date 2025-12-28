# Fix for Build Errors in Intersect.Editor

## Issue
The build was failing with errors related to missing `Microsoft.Data.Sqlite` namespace and types.

## Root Cause
The NuGet packages (`Microsoft.Data.Sqlite` and `SQLitePCLRaw.bundle_e_sqlite3`) were not properly restored on your machine.

## Changes Made
1. Updated `Microsoft.Data.Sqlite` package version from 8.0.0 to 8.0.11 (to match other projects)
2. Added `NuGet.config` to ensure proper package restoration
3. Created helper scripts for easy restoration and building

## How to Fix

### Option 1: Using Visual Studio
1. Open the solution in Visual Studio
2. Right-click on the solution in Solution Explorer
3. Select "Restore NuGet Packages"
4. Rebuild the solution

### Option 2: Using the Command Line
Run one of the provided scripts from the repository root:

**PowerShell:**
```powershell
.\restore-and-build-editor.ps1
```

**Command Prompt:**
```cmd
restore-and-build-editor.bat
```

### Option 3: Using dotnet CLI
```bash
dotnet restore Intersect.Editor/Intersect.Editor.csproj
dotnet build Intersect.Editor/Intersect.Editor.csproj
```

## Verification
After running any of the above options, the build should complete successfully without the `CS0234` and `CS0246` errors.

The warning about `Mono.Data.Sqlite.Portable` being incompatible is expected and can be ignored as we're now using `Microsoft.Data.Sqlite` instead.
