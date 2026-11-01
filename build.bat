@echo off
setlocal

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo csc.exe not found. .NET Framework 4.x is required to build.
  exit /b 1
)

if not exist dist mkdir dist

"%CSC%" /nologo /target:winexe /optimize+ /out:dist\LunarAccountManager.exe ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Web.Extensions.dll ^
  src\*.cs

if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

echo BUILD OK -^> dist\LunarAccountManager.exe
