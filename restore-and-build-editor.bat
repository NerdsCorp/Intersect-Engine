@echo off
REM Batch script to restore NuGet packages and build the Intersect Editor
REM This script ensures all dependencies are properly restored before building

echo Restoring NuGet packages for Intersect.Editor...
dotnet restore "%~dp0Intersect.Editor\Intersect.Editor.csproj"

if %ERRORLEVEL% EQU 0 (
    echo Packages restored successfully!
    echo Building Intersect.Editor...
    dotnet build "%~dp0Intersect.Editor\Intersect.Editor.csproj" --configuration Debug

    if %ERRORLEVEL% EQU 0 (
        echo Build completed successfully!
    ) else (
        echo Build failed. Please check the error messages above.
        exit /b 1
    )
) else (
    echo Package restore failed. Please check your internet connection and NuGet configuration.
    exit /b 1
)
