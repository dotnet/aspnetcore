@echo off
setlocal enabledelayedexpansion

:: Identity V3 Schema - Docker Quick Start Script

:: Set environment variable to bypass OpenSSL 3.x compatibility issues
set OPENSSL_CONF=/dev/null

:MENU
cls
echo ==========================================
echo Identity V3 Schema - Docker Setup
echo ==========================================
echo.

:: Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not running!
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)

echo [OK] Docker is running
echo.

echo Choose an option:
echo.
echo   1) Start Everything (SQL Server + Web App)
echo   2) Start SQL Server Only
echo   3) Stop All Containers
echo   4) Stop and Remove All Data
echo   5) View Logs
echo   6) Check Database Schema
echo   7) Connect to SQL Server (sqlcmd)
echo   8) Rebuild and Restart
echo   9) Exit
echo.

set /p choice="Enter option [1-9]: "

if "%choice%"=="1" goto START_ALL
if "%choice%"=="2" goto START_SQL
if "%choice%"=="3" goto STOP_ALL
if "%choice%"=="4" goto REMOVE_ALL
if "%choice%"=="5" goto VIEW_LOGS
if "%choice%"=="6" goto CHECK_SCHEMA
if "%choice%"=="7" goto CONNECT_SQL
if "%choice%"=="8" goto REBUILD
if "%choice%"=="9" goto EXIT
goto INVALID

:START_ALL
echo.
echo [*] Starting SQL Server and Web Application...
docker-compose up --build -d
echo.
echo Waiting for SQL Server to be ready...
timeout /t 5 >nul

set attempts=0
:WAIT_SQL_ALL
docker exec identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -Q "SELECT 1" >nul 2>&1
if errorlevel 1 (
    set /a attempts+=1
    if !attempts! geq 12 (
        echo [ERROR] SQL Server did not start in time
        pause
        goto MENU
    )
    echo Waiting... (attempt !attempts!/12)
    timeout /t 5 >nul
    goto WAIT_SQL_ALL
)

echo.
echo [OK] Containers started!
echo.
echo Web Application: http://localhost:5000
echo SQL Server: localhost:1433
echo    Username: sa
echo    Password: YourStrong@Passw0rd
echo    Database: IdentitySample_V3
echo.
pause
goto MENU

:START_SQL
echo.
echo [*] Starting SQL Server only...
docker-compose up sqlserver -d
echo.
echo Waiting for SQL Server to be ready...
timeout /t 5 >nul

set attempts=0
:WAIT_SQL
docker exec identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -Q "SELECT 1" >nul 2>&1
if errorlevel 1 (
    set /a attempts+=1
    if !attempts! geq 12 (
        echo [ERROR] SQL Server did not start in time
        pause
        goto MENU
    )
    echo Waiting... (attempt !attempts!/12)
    timeout /t 5 >nul
    goto WAIT_SQL
)

echo.
echo [OK] SQL Server started!
echo.
echo Connection details:
echo   Server: localhost,1433
echo   Username: sa
echo   Password: YourStrong@Passw0rd
echo.
echo To run the web app locally:
echo   dotnet run
echo.
pause
goto MENU

:STOP_ALL
echo.
echo [*] Stopping all containers...
docker-compose stop
echo [OK] Containers stopped
echo.
pause
goto MENU

:REMOVE_ALL
echo.
echo [WARNING] This will remove all data!
set /p confirm="Are you sure? (yes/no): "
if /i "%confirm%"=="yes" (
    echo [*] Stopping and removing containers and volumes...
    docker-compose down -v
    echo [OK] All containers and data removed
) else (
    echo Cancelled
)
echo.
pause
goto MENU

:VIEW_LOGS
echo.
echo [*] Viewing logs (Ctrl+C to exit)...
echo.
docker-compose logs -f
goto MENU

:CHECK_SCHEMA
echo.
echo [*] Checking V3 Schema...
echo.

echo 1. Checking if database exists...
docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -Q "SELECT name FROM sys.databases WHERE name = 'IdentitySample_V3'"

echo.
echo 2. Checking AspNetUserLogins columns (should include 'Id')...
docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -d IdentitySample_V3 -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'AspNetUserLogins' ORDER BY ORDINAL_POSITION"

echo.
echo 3. Checking primary key on AspNetUserLogins (should be 'Id')...
docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -d IdentitySample_V3 -Q "EXEC sp_pkeys 'AspNetUserLogins'"

echo.
pause
goto MENU

:CONNECT_SQL
echo.
echo [*] Connecting to SQL Server...
echo    (Type 'EXIT' to return to menu)
echo.
docker exec -it identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -d IdentitySample_V3
goto MENU

:REBUILD
echo.
echo [*] Rebuilding and restarting...
docker-compose down
docker-compose up --build -d
echo.
echo Waiting for SQL Server to be ready...
timeout /t 5 >nul

set attempts=0
:WAIT_SQL_REBUILD
docker exec identity-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -No -Q "SELECT 1" >nul 2>&1
if errorlevel 1 (
    set /a attempts+=1
    if !attempts! geq 12 (
        echo [ERROR] SQL Server did not start in time
        pause
        goto MENU
    )
    echo Waiting... (attempt !attempts!/12)
    timeout /t 5 >nul
    goto WAIT_SQL_REBUILD
)

echo.
echo [OK] Rebuild complete!
echo Web Application: http://localhost:5000
echo.
pause
goto MENU

:INVALID
echo.
echo [ERROR] Invalid option. Please try again.
echo.
timeout /t 2 >nul
goto MENU

:EXIT
echo.
echo Goodbye!
exit /b 0
