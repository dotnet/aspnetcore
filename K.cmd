@ECHO OFF
 
SETLOCAL ENABLEDELAYEDEXPANSION
 
SET CURRDIR=%CD%
SET PARENTDIR=!CURRDIR!
 
:START
IF EXIST !CURRDIR!\packages\ProjectK* FOR /F %%I IN ('DIR !CURRDIR!\packages\ProjectK* /B /O:-D') DO (SET ProjectKDir=%%I& GOTO :ENDFOR) 
:ENDFOR
 
SET LocalKCmd=!CURRDIR!\packages\!ProjectKDir!\tools\k.cmd
 
IF NOT EXIST !LocalKCmd! (
    CALL :RESOLVE "!CURRDIR!\.." PARENTDIR
    IF !CURRDIR!==!PARENTDIR! (
        ECHO Unable to locate the ProjectK runtime
        ENDLOCAL & EXIT /b 1
    ) ELSE (
        SET CURRDIR=!PARENTDIR!
        GOTO :START
    )
)
 
CALL "!LocalKCmd!" %*
ENDLOCAL & EXIT /b %ERRORLEVEL%
 
:RESOLVE
SET %2=%~f1
GOTO :EOF