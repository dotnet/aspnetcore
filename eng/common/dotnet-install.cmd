@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0dotnet-install.ps1""" %*"