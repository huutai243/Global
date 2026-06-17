@echo off
setlocal EnableExtensions

pushd "%~dp0"

set "SOLUTION=ECommerce.sln"
set "EF_VERSION=8.0.6"

set "LOG_DIR=Logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "TIMESTAMP=%%I"
set "LOG_FILE=%LOG_DIR%\setup-dev-%TIMESTAMP%.log"

call :log "Starting development setup."
call :log "Working directory: %CD%"
call :log "Log file: %LOG_FILE%"

if not exist "%SOLUTION%" (
    call :fail "Solution file was not found: %SOLUTION%. Put this .bat next to ECommerce.sln."
    exit /b 1
)

set "API_GATEWAY_PROJECT=ApiGateway\ECommerce.ApiGateway\ECommerce.ApiGateway.csproj"

set "IDENTITY_WEB_PROJECT=Services\Identity\ECommerce.Identity.WebApi\ECommerce.Identity.WebApi.csproj"
set "CATALOG_WEB_PROJECT=Services\Catalog\ECommerce.Catalog.WebApi\ECommerce.Catalog.WebApi.csproj"
set "CART_WEB_PROJECT=Services\Cart\ECommerce.Cart.WebApi\ECommerce.Cart.WebApi.csproj"
set "ORDERING_WEB_PROJECT=Services\Ordering\ECommerce.Ordering.WebApi\ECommerce.Ordering.WebApi.csproj"
set "INVENTORY_WEB_PROJECT=Services\Inventory\ECommerce.Inventory.WebApi\ECommerce.Inventory.WebApi.csproj"
set "PAYMENT_WEB_PROJECT=Services\Payment\ECommerce.Payment.WebApi\ECommerce.Payment.WebApi.csproj"

set "ORDERING_WORKER_PROJECT=Services\Ordering\ECommerce.Ordering.Worker\ECommerce.Ordering.Worker.csproj"
set "INVENTORY_WORKER_PROJECT=Services\Inventory\ECommerce.Inventory.Worker\ECommerce.Inventory.Worker.csproj"

set "IDENTITY_MIGRATION_PROJECT=Services\Identity\ECommerce.Identity.Infrastructure\ECommerce.Identity.Infrastructure.csproj"
set "CATALOG_MIGRATION_PROJECT=Services\Catalog\ECommerce.Catalog.Infrastructure\ECommerce.Catalog.Infrastructure.csproj"
set "CART_MIGRATION_PROJECT=Services\Cart\ECommerce.Cart.Infrastructure\ECommerce.Cart.Infrastructure.csproj"
set "ORDERING_MIGRATION_PROJECT=Services\Ordering\ECommerce.Ordering.Infrastructure\ECommerce.Ordering.Infrastructure.csproj"
set "INVENTORY_MIGRATION_PROJECT=Services\Inventory\ECommerce.Inventory.Infrastructure\ECommerce.Inventory.Infrastructure.csproj"
set "PAYMENT_MIGRATION_PROJECT=Services\Payment\ECommerce.Payment.Infrastructure\ECommerce.Payment.Infrastructure.csproj"

call :ensure_dotnet
if errorlevel 1 exit /b 1

call :ensure_dotnet_ef
if errorlevel 1 exit /b 1

call :run dotnet restore "%SOLUTION%"
if errorlevel 1 (
    call :fail "dotnet restore failed."
    exit /b 1
)

call :run dotnet build "%SOLUTION%"
if errorlevel 1 (
    call :fail "dotnet build failed."
    exit /b 1
)

call :update_database "Identity" "%IDENTITY_MIGRATION_PROJECT%" "%IDENTITY_WEB_PROJECT%" "IdentityDbContext"
if errorlevel 1 exit /b 1

call :update_database "Catalog" "%CATALOG_MIGRATION_PROJECT%" "%CATALOG_WEB_PROJECT%" "CatalogDbContext"
if errorlevel 1 exit /b 1

call :update_database "Cart" "%CART_MIGRATION_PROJECT%" "%CART_WEB_PROJECT%" "CartDbContext"
if errorlevel 1 exit /b 1

call :update_database "Ordering" "%ORDERING_MIGRATION_PROJECT%" "%ORDERING_WEB_PROJECT%" "OrderingDbContext"
if errorlevel 1 exit /b 1

call :update_database "Inventory" "%INVENTORY_MIGRATION_PROJECT%" "%INVENTORY_WEB_PROJECT%" "InventoryDbContext"
if errorlevel 1 exit /b 1

call :update_database "Payment" "%PAYMENT_MIGRATION_PROJECT%" "%PAYMENT_WEB_PROJECT%" "PaymentDbContext"
if errorlevel 1 exit /b 1

call :log "Database setup completed successfully."
call :log "Starting microservices."

start "Identity WebApi" cmd /k dotnet run --project "%IDENTITY_WEB_PROJECT%"
start "Catalog WebApi" cmd /k dotnet run --project "%CATALOG_WEB_PROJECT%"
start "Cart WebApi" cmd /k dotnet run --project "%CART_WEB_PROJECT%"
start "Ordering WebApi" cmd /k dotnet run --project "%ORDERING_WEB_PROJECT%"
start "Ordering Worker" cmd /k dotnet run --project "%ORDERING_WORKER_PROJECT%"
start "Inventory WebApi" cmd /k dotnet run --project "%INVENTORY_WEB_PROJECT%"
start "Inventory Worker" cmd /k dotnet run --project "%INVENTORY_WORKER_PROJECT%"
start "Payment WebApi" cmd /k dotnet run --project "%PAYMENT_WEB_PROJECT%"
start "ApiGateway" cmd /k dotnet run --project "%API_GATEWAY_PROJECT%"

call :log "Started services."
call :log "Frontend should call: http://localhost:5000/api"

popd
exit /b 0

:ensure_dotnet
call :log "Checking dotnet SDK."
call :run dotnet --version
if errorlevel 1 (
    call :fail "dotnet SDK was not found. Install .NET 8 SDK first."
    exit /b 1
)

exit /b 0

:ensure_dotnet_ef
call :log "Installing or updating dotnet-ef %EF_VERSION%."

call :run dotnet tool update --global dotnet-ef --version %EF_VERSION%
if errorlevel 1 (
    call :log "dotnet-ef update failed. Trying install instead."

    call :run dotnet tool install --global dotnet-ef --version %EF_VERSION%
    if errorlevel 1 (
        call :fail "dotnet-ef setup failed."
        exit /b 1
    )
)

exit /b 0

:update_database
set "SERVICE_NAME=%~1"
set "MIGRATION_PROJECT_PATH=%~2"
set "STARTUP_PROJECT_PATH=%~3"
set "DB_CONTEXT=%~4"

call :log "Updating %SERVICE_NAME% database."
call :log "%SERVICE_NAME% migration project: %MIGRATION_PROJECT_PATH%"
call :log "%SERVICE_NAME% startup project: %STARTUP_PROJECT_PATH%"
call :log "%SERVICE_NAME% DbContext: %DB_CONTEXT%"

if "%SERVICE_NAME%"=="" (
    call :fail "Service name is empty."
    exit /b 1
)

if "%MIGRATION_PROJECT_PATH%"=="" (
    call :fail "%SERVICE_NAME% migration project path is empty."
    exit /b 1
)

if "%STARTUP_PROJECT_PATH%"=="" (
    call :fail "%SERVICE_NAME% startup project path is empty."
    exit /b 1
)

if "%DB_CONTEXT%"=="" (
    call :fail "%SERVICE_NAME% DbContext name is empty."
    exit /b 1
)

if not exist "%MIGRATION_PROJECT_PATH%" (
    call :fail "%SERVICE_NAME% migration project was not found: %MIGRATION_PROJECT_PATH%"
    exit /b 1
)

if not exist "%STARTUP_PROJECT_PATH%" (
    call :fail "%SERVICE_NAME% startup project was not found: %STARTUP_PROJECT_PATH%"
    exit /b 1
)

call :run dotnet ef database update --context "%DB_CONTEXT%" --project "%MIGRATION_PROJECT_PATH%" --startup-project "%STARTUP_PROJECT_PATH%"
if errorlevel 1 (
    call :fail "%SERVICE_NAME% database update failed."
    exit /b 1
)

call :log "%SERVICE_NAME% database updated."
exit /b 0

:run
set "SETUP_COMMAND=%*"
call :log "> %SETUP_COMMAND%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "cmd /d /c $env:SETUP_COMMAND 2>&1 | Tee-Object -FilePath $env:LOG_FILE -Append; exit $LASTEXITCODE"

exit /b %ERRORLEVEL%

:log
echo %~1
>> "%LOG_FILE%" echo [%DATE% %TIME%] %~1
exit /b 0

:fail
call :log "ERROR: %~1"
call :log "Setup stopped."
exit /b 1