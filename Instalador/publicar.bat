@echo off
REM ════════════════════════════════════════════════════════════════
REM  Hotel Rydl - genera el instalador
REM  1) Publica el programa (self-contained: no hace falta instalar .NET)
REM  2) Compila HotelRydl_Instalador.iss con Inno Setup 6
REM  Resultado: Instalador\salida\Instalador_HotelRydl_v3.0.exe
REM ════════════════════════════════════════════════════════════════
cd /d "%~dp0.."

echo.
echo [1/2] Publicando el programa...
if exist "Instalador\publicar" rmdir /s /q "Instalador\publicar"
set PROY=
for /f "delims=" %%f in ('dir /s /b "GUI HRS-480.csproj" 2^>nul ^| findstr /v /i "\\obj\\ \\bin\\"') do if not defined PROY set "PROY=%%f"
if not defined PROY (
  echo No se encontro "GUI HRS-480.csproj" debajo de %CD%
  pause & exit /b 1
)
echo Proyecto: %PROY%
dotnet publish "%PROY%" -c Release -r win-x64 --self-contained true -o "Instalador\publicar"
if errorlevel 1 (
  echo ERROR al publicar. Revisa que compile en Visual Studio.
  pause & exit /b 1
)

echo.
echo [2/2] Compilando el instalador...
set ISCC="%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist %ISCC% set ISCC="%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist %ISCC% (
  echo No se encontro Inno Setup 6. Abri Instalador\HotelRydl_Instalador.iss con Inno Setup y presiona F9.
  pause & exit /b 1
)
%ISCC% "Instalador\HotelRydl_Instalador.iss"
if errorlevel 1 ( echo ERROR al compilar el instalador. & pause & exit /b 1 )

echo.
echo Listo: Instalador\salida\Instalador_HotelRydl_v3.0.exe
pause
