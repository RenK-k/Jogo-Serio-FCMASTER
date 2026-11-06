@echo off
title FootySim - Match Engine
cd /d "%~dp0"

echo ==========================================
echo    FootySim
echo    Spatial Agent-Based Match Engine
echo ==========================================
echo.

set "PY="
where python >nul 2>nul && set "PY=python"
if not defined PY (
    where py >nul 2>nul && set "PY=py"
)
if not defined PY (
    echo [!] Python is not installed, or not on your PATH.
    echo     Install it from https://python.org and tick
    echo     "Add Python to PATH" during setup.
    echo.
    pause
    exit /b 1
)

echo What do you want to run?
echo.
echo   [1] Simulate a match  - writes match_output.json
echo   [2] Run the test suite
echo.
set "MODE="
set /p MODE=Choose 1 or 2 (just press Enter to simulate):
if "%MODE%"=="2" goto tests
goto match

:match
echo.
echo Simulating a full 90-minute match...
echo (54,000 ticks - this takes a moment)
echo.
%PY% example.py
echo.
echo Done. Results written to match_output.json
goto done

:tests
echo.
echo Running the acceptance tests...
echo.
%PY% -m pytest tests/ -q
goto done

:done
echo.
pause
