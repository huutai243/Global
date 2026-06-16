@echo off
setlocal EnableExtensions

set "SOLUTION=ECommerce.sln"

set "EF_VERSION=8.0.6"

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

if not exist "Logs" mkdir "Logs"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "TIMESTAMP=%%I"
set "LOG_FILE=Logs\setup-dev-%TIMESTAMP%.log"

call :log "Starting development setup."
call :log "Log file: %LOG_FILE%"

call :ensure_dotnet
call :ensure_dotnet_ef

call :run dotnet restore "%SOLUTION%"
if errorlevel 1 call :fail "dotnet restore failed. Please check the log for details."

call :run dotnet build "%SOLUTION%"
if errorlevel 1 call :fail "dotnet build failed. Please check the log for details."

call :update_database "Identity" "%IDENTITY_MIGRATION_PROJECT%" "%IDENTITY_WEB_PROJECT%" "IdentityDbContext"
call :update_database "Catalog" "%CATALOG_MIGRATION_PROJECT%" "%CATALOG_WEB_PROJECT%" "CatalogDbContext"
call :update_database "Cart" "%CART_MIGRATION_PROJECT%" "%CART_WEB_PROJECT%" "CartDbContext"
call :update_database "Ordering" "%ORDERING_MIGRATION_PROJECT%" "%ORDERING_WEB_PROJECT%" "OrderingDbContext"
call :update_database "Inventory" "%INVENTORY_MIGRATION_PROJECT%" "%INVENTORY_WEB_PROJECT%" "InventoryDbContext"
call :update_database "Payment" "%PAYMENT_MIGRATION_PROJECT%" "%PAYMENT_WEB_PROJECT%" "PaymentDbContext"

call :log "Setup completed successfully. Starting microservice development environment."

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

exit /b 0

:ensure_dotnet
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

exit /b 0

:ensure_dotnet_ef
call :log "Installing or updating dotnet-ef %EF_VERSION%."

call :run dotnet tool update --global dotnet-ef --version %EF_VERSION%
if errorlevel 1 (
    call :log "dotnet-ef update failed. Trying install instead."

    call :run dotnet tool install --global dotnet-ef --version %EF_VERSION%
    if errorlevel 1 (
        call :fail "dotnet-ef setup failed. Please check your .NET SDK installation and global tool path."
    )
)

exit /b 0

:update_database
set "SERVICE_NAME=%~1"
set "MIGRATION_PROJECT_PATH=%~2"
set "STARTUP_PROJECT_PATH=%~3"
set "DB_CONTEXT=%~4"

call :log "Updating %SERVICE_NAME% database."

if not exist "%MIGRATION_PROJECT_PATH%" (
    call :fail "%SERVICE_NAME% migration project was not found: %MIGRATION_PROJECT_PATH%"
)

if not exist "%STARTUP_PROJECT_PATH%" (
    call :fail "%SERVICE_NAME% startup project was not found: %STARTUP_PROJECT_PATH%"
)

call :run dotnet ef database update --context "%DB_CONTEXT%" --project "%MIGRATION_PROJECT_PATH%" --startup-project "%STARTUP_PROJECT_PATH%"
if errorlevel 1 (
    call :fail "%SERVICE_NAME% database update failed. Check connection string, SQL Server service, migrations, and project references."
)

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