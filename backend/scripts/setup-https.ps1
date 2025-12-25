# PowerShell script to set up and trust the .NET development HTTPS certificate
Write-Host "Setting up HTTPS development certificate..." -ForegroundColor Cyan

# Clean existing certificates
Write-Host "`nCleaning existing certificates..." -ForegroundColor Yellow
dotnet dev-certs https --clean

# Generate and trust the certificate
Write-Host "`nGenerating and trusting development certificate..." -ForegroundColor Yellow
$result = dotnet dev-certs https --trust

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ HTTPS certificate has been set up and trusted!" -ForegroundColor Green
    Write-Host "`nYou may need to restart your browser for the changes to take effect." -ForegroundColor Yellow
} else {
    Write-Host "`n⚠ Certificate trust may require administrator privileges." -ForegroundColor Yellow
    Write-Host "If you see a UAC prompt, please approve it." -ForegroundColor Yellow
    Write-Host "`nAlternatively, run this command manually as administrator:" -ForegroundColor Cyan
    Write-Host "  dotnet dev-certs https --trust" -ForegroundColor White
}

Write-Host "`nVerifying certificate..." -ForegroundColor Yellow
dotnet dev-certs https --check

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✓ Certificate is valid and trusted!" -ForegroundColor Green
} else {
    Write-Host "`n⚠ Certificate verification failed. Please run the trust command manually." -ForegroundColor Red
}

