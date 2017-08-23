@ECHO OFF
PowerShell -NoProfile -NoLogo -ExecutionPolicy unrestricted -Command "& '%~dp0dotnet-install.ps1' %*; exit $LASTEXITCODE"
