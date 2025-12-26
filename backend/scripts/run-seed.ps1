# PowerShell script to run the database seeding script
Write-Host "=== Database Seeding Script ===" -ForegroundColor Cyan
Write-Host ""

# Change to project root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

# Run the seeding script
Write-Host "Running seeding script..." -ForegroundColor Yellow
dotnet run --project taskedin-be.csproj -- seed

Write-Host ""
Write-Host "Seeding complete!" -ForegroundColor Green

