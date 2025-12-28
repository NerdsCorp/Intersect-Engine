# PowerShell script to restore NuGet packages and build the Intersect Editor
# This script ensures all dependencies are properly restored before building

Write-Host "Restoring NuGet packages for Intersect.Editor..." -ForegroundColor Green
dotnet restore "$PSScriptRoot\Intersect.Editor\Intersect.Editor.csproj"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Packages restored successfully!" -ForegroundColor Green
    Write-Host "Building Intersect.Editor..." -ForegroundColor Green
    dotnet build "$PSScriptRoot\Intersect.Editor\Intersect.Editor.csproj" --configuration Debug

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Build failed. Please check the error messages above." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Package restore failed. Please check your internet connection and NuGet configuration." -ForegroundColor Red
    exit 1
}
