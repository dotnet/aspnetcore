@echo off
if not defined TemplatesRoot (
    echo Initializing templates environment
    call %~dp0\TemplatesEnv.cmd
)