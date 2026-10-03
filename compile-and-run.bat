@echo off
setlocal enabledelayedexpansion

:: ============================================================================
:: BSAT: Compile TypeScript to Node.js & Execute Immediately
:: ============================================================================

title BSAT TypeScript Compiler & Runner
color 0F

echo [*] Compiling server/index.ts -> dist-server/index.js...
call npx tsc -p tsconfig.server.json

if %ERRORLEVEL% neq 0 (
    color 0C
    echo [ERROR] TypeScript compilation encountered errors.
    pause
    exit /b 1
)

echo [OK] Compilation complete! Launching Node.js...
echo ----------------------------------------------------
node dist-server/index.js

pause
endlocal
