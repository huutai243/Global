@echo off
setlocal EnableExtensions

set "SOLUTION=ECommerce.sln"

set "API_GATEWAY_PROJECT=ApiGateway\ECommerce.ApiGateway\ECommerce.ApiGateway.csproj"

set "IDENTITY_PROJECT=Services\Identity\ECommerce.Identity.WebApi\ECommerce.Identity.WebApi.csproj"
set "CATALOG_PROJECT=Services\Catalog\ECommerce.Catalog.WebApi\ECommerce.Catalog.WebApi.csproj"
set "CART_PROJECT=Services\Cart\ECommerce.Cart.WebApi\ECommerce.Cart.WebApi.csproj"

set "MIGRATION_PROJECT=Infras\ECommerce.Infrastructure.Persistence\ECommerce.Infrastructure.Persistence.csproj"
set "MIGRATION_STARTUP_PROJECT=Services\Identity\ECommerce.Identity.WebApi\ECommerce.Identity.WebApi.csproj"

set "EF_VERSION=8.0.6"

if not exist "Logs" mkdir "Logs"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "TIMESTAMP=%%I"
set "LOG_FILE=Logs\setup-dev-%TIMESTAMP%.log"

call :log "Starting development setup."
call :log "Log file: %LOG_FILE%"

call :run dotnet --version
if errorlevel 1 (
    call :log "dotnet SDK was not found. Trying to install .NET 8 SDK with winget."

    call :run winget --version
    if errorlevel 1 (
        call :fail "dotnet SDK is missing, and winget is not available. Install the .NET 8 SDK manually, then run setup-dev.bat again."
    )

    call :run winget install Microsoft.DotNet.SDK.8 --accept-package-agreements --accept-source-agreements
    if errorlevel 1 (
        call :fail ".NET 8 SDK installation failed. Install the .NET 8 SDK manually, then run setup-dev.bat again."
    )

    call :run dotnet --version
    if errorlevel 1 (
        call :fail "dotnet SDK is still not available after installation. Open a new terminal or install the .NET 8 SDK manually, then run setup-dev.bat again."
    )
)

call :log "Installing or updating dotnet-ef %EF_VERSION%."
call :run dotnet tool update --global dotnet-ef --version %EF_VERSION%
if errorlevel 1 (
    call :log "dotnet-ef update failed. Trying install instead."

    call :run dotnet tool install --global dotnet-ef --version %EF_VERSION%
    if errorlevel 1 (
        call :fail "dotnet-ef setup failed. Please check your .NET SDK installation and global tool path."
    )
)

call :run dotnet restore "%SOLUTION%"
if errorlevel 1 call :fail "dotnet restore failed. Please check the log for details."

call :run dotnet build "%SOLUTION%"
if errorlevel 1 call :fail "dotnet build failed. Please check the log for details."

call :run dotnet ef database update --project "%MIGRATION_PROJECT%" --startup-project "%MIGRATION_STARTUP_PROJECT%"
if errorlevel 1 (
    call :fail "Database update failed. Please check SQL Server service, ConnectionStrings:ECommerceConnection, and database permissions."
)

call :log "Setup completed successfully. Starting microservice development environment."

start "Identity WebApi" cmd /k dotnet run --project "%IDENTITY_PROJECT%"
start "Catalog WebApi" cmd /k dotnet run --project "%CATALOG_PROJECT%"
start "Cart WebApi" cmd /k dotnet run --project "%CART_PROJECT%"
start "ApiGateway" cmd /k dotnet run --project "%API_GATEWAY_PROJECT%"

call :log "Started services:"
call :log "Identity WebApi: http://localhost:5212"
call :log "Catalog WebApi: http://localhost:5018"
call :log "Cart WebApi: http://localhost:5137"
call :log "ApiGateway: http://localhost:5000"
call :log "Frontend should call: http://localhost:5000/api"

exit /b 0

:run
set "COMMAND=%*"
call :log "> %COMMAND%"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& { & '%ComSpec%' /d /c $env:COMMAND 2>&1 | Tee-Object -FilePath $env:LOG_FILE -Append; exit $LASTEXITCODE }"
exit /b %ERRORLEVEL%

:log
echo %~1
>> "%LOG_FILE%" echo [%DATE% %TIME%] %~1
exit /b 0

:fail
call :log "ERROR: %~1"
call :log "Setup stopped."
exit /b 1