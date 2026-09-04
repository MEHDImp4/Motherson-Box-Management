@echo off
setlocal
cd /d "%~dp0"

REM ============================================================
REM  Motherson Box Management - Docker launcher
REM  Starts the full stack (SQL Server + web) via docker compose.
REM ============================================================

REM Guard against the weak machine-level DEMO_AD001_PASSWORD value.
REM The app requires 3 distinct demo passwords of at least 16 chars.
set "DEMO_AD001_PASSWORD=AD001-Dev-Admin-Pass-2026!!"

REM Create .env from the template if it does not exist yet.
if not exist ".env" (
    echo Creating .env from .env.example ...
    copy ".env.example" ".env" >nul
    echo [!] Edit .env and set a strong MSSQL_SA_PASSWORD before continuing.
    exit /b 1
)

echo.
echo Starting Motherson Box Management stack (Docker)...
echo Building and starting containers...
docker compose up -d --build
if errorlevel 1 (
    echo.
    echo ERROR: docker compose up failed. Check the output above.
    exit /b 1
)

echo.
echo Waiting for services to become healthy...
timeout /t 8 /nobreak >nul

echo.
echo The service status:
docker compose ps

echo.
echo ============================================================
echo  App is available at:  http://localhost:8080
echo ============================================================
echo.
echo Demo login accounts (Development seed):
echo   OP001  Operator       -> OpOperatorPass1!
echo   SP001  Supervisor     -> SpSupervisorPass2@
echo   AD001  Administrator  -> AD001-Dev-Admin-Pass-2026!!
echo.
start "" "http://localhost:8080"

endlocal
