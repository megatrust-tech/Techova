# PowerShell script to generate secure JWT secrets
# Usage: .\scripts\generate-jwt-secret.ps1

Write-Host "Generating secure JWT secrets..." -ForegroundColor Cyan
Write-Host ""

# Generate 32-byte (256-bit) secrets for HS256
$accessTokenSecret = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
$refreshTokenSecret = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

Write-Host "Add these to your .env file:" -ForegroundColor Yellow
Write-Host ""
Write-Host "JWT_ACCESS_TOKEN_SECRET=$accessTokenSecret" -ForegroundColor Green
Write-Host "JWT_REFRESH_TOKEN_SECRET=$refreshTokenSecret" -ForegroundColor Green
Write-Host ""
Write-Host "Or copy them directly:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Access Token Secret:" -ForegroundColor Cyan
Write-Host $accessTokenSecret -ForegroundColor White
Write-Host ""
Write-Host "Refresh Token Secret:" -ForegroundColor Cyan
Write-Host $refreshTokenSecret -ForegroundColor White
Write-Host ""

