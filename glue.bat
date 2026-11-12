@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

set "OUT_FILE=AllCode.txt"
set "ROOT_DIR=%CD%\"

if exist "%OUT_FILE%" del "%OUT_FILE%"

for /r %%F in (*.cs) do (
    echo "%%F" | findstr /i "\\obj\\ \\bin\\" > nul
    if errorlevel 1 (
        set "FULL_PATH=%%F"
        set "REL_PATH=!FULL_PATH:%ROOT_DIR%=!"
        
        echo //начало !REL_PATH!>> "%OUT_FILE%"
        type "%%F">> "%OUT_FILE%"
        echo.>> "%OUT_FILE%"
        echo //конец !REL_PATH!>> "%OUT_FILE%"
        echo.>> "%OUT_FILE%"
    )
)

echo Готово! Весь код собран в файл %OUT_FILE%.
pause
