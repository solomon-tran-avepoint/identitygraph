@echo off
echo Starting Azure AD + Duende IdentityServer + MVC Client Demo
echo.
echo This will start:
echo - IdentityServer on https://localhost:5001
echo - MVC Client on https://localhost:5002
echo.
echo Make sure you have configured Azure AD settings in IdentityServer/appsettings.json
echo.
pause

echo Starting IdentityServer...
start "IdentityServer" cmd /k "cd IdentityServer && dotnet run --urls https://localhost:5001"

timeout /t 3 /nobreak > nul

echo Starting MVC Client...
start "MVC Client" cmd /k "cd MvcClient && dotnet run --urls https://localhost:5002"

echo.
echo Both applications are starting...
echo - IdentityServer: https://localhost:5001
echo - MVC Client: https://localhost:5002
echo.
echo Press any key to exit...
pause > nul
