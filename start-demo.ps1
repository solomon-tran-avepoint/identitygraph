Write-Host "Starting Azure AD + Duende IdentityServer + MVC Client Demo" -ForegroundColor Green
Write-Host ""
Write-Host "This will start:"
Write-Host "- IdentityServer on https://localhost:5001" -ForegroundColor Yellow
Write-Host "- MVC Client on https://localhost:5002" -ForegroundColor Yellow
Write-Host ""
Write-Host "Make sure you have configured Azure AD settings in IdentityServer/appsettings.json" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to continue"

Write-Host "Starting IdentityServer..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd IdentityServer; dotnet run --urls https://localhost:5001"

Start-Sleep -Seconds 3

Write-Host "Starting MVC Client..." -ForegroundColor Green  
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd MvcClient; dotnet run --urls https://localhost:5002"

Write-Host ""
Write-Host "Both applications are starting..." -ForegroundColor Green
Write-Host "- IdentityServer: https://localhost:5001" -ForegroundColor Yellow
Write-Host "- MVC Client: https://localhost:5002" -ForegroundColor Yellow
Write-Host ""
Write-Host "Navigate to https://localhost:5002 to test the authentication flow" -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"
