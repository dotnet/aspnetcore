@ECHO OFF
IF "%1"=="" (
  CALL :cmd_help
) ELSE (
  CALL :cmd_%1 %*
  IF ERRORLEVEL 1 CALL:cmd_help
)
GOTO:EOF


:cmd_setup
SET "_KVM_PATH=%USERPROFILE%\.k\"
SET "_TEMP_PATH=%PATH%"

IF /I NOT "%~dp0"=="%_KVM_PATH%" (
  IF NOT EXIST "%_KVM_PATH%" MKDIR "%_KVM_PATH%"
  COPY "%~f0" "%_KVM_PATH%kvm.cmd"
)

:PARSE_START
@IF "%_TEMP_PATH%"=="" GOTO PARSE_END
@FOR /F "tokens=1* delims=;" %%a in ("%_TEMP_PATH%") Do @IF "%%a"=="%_KVM_PATH%" GOTO:end_setup
@FOR /F "tokens=1* delims=;" %%a in ("%_TEMP_PATH%") Do @SET _TEMP_PATH=%%b
@GOTO PARSE_START
:PARSE_END

SET "PATH=%PATH%;%_KVM_PATH%"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Environment]::SetEnvironmentVariable('PATH',[Environment]::GetEnvironmentVariable('PATH','user')+';%_KVM_PATH%','user');"


:end_setup
CALL "%_KVM_PATH%kvm.cmd" upgrade
@ECHO Running crossgen, see crossgen.log for results
CALL "%_KVM_PATH%k.cmd" crossgen >crossgen.log 2>crossgen.err.log
SET _KVM_PATH=
SET _TEMP_PATH=
GOTO:EOF


:cmd_upgrade
CALL:cmd_install install
CALL:cmd_alias alias default %_KVM_VERSION%
GOTO:EOF


:cmd_install
IF NOT EXIST "%~dp0.nuget\NuGet.exe" (
  IF NOT EXIST "%~dp0.nuget" MKDIR "%~dp0.nuget"
  ECHO Downloading latest version of NuGet.exe...
  @powershell -NoProfile -ExecutionPolicy unrestricted -Command "((new-object net.webclient).DownloadFile('https://nuget.org/nuget.exe', '%~dp0.nuget\NuGet.exe'))"
)

IF NOT EXIST "%~dp0.nuget\NuGet.config" (
echo ^<configuration^> >"%~dp0.nuget\NuGet.config"
echo   ^<packageSources^> >>"%~dp0.nuget\NuGet.config"
echo     ^<add key="AspNetVNext" value="https://www.myget.org/F/aspnetvnext/api/v2" /^> >>"%~dp0.nuget\NuGet.config"
echo   ^</packageSources^> >>"%~dp0.nuget\NuGet.config"
echo   ^<packageSourceCredentials^> >>"%~dp0.nuget\NuGet.config"
echo     ^<AspNetVNext^> >>"%~dp0.nuget\NuGet.config"
echo       ^<add key="Username" value="aspnetreadonly" /^> >>"%~dp0.nuget\NuGet.config"
echo       ^<add key="ClearTextPassword" value="4d8a2d9c-7b80-4162-9978-47e918c9658c" /^> >>"%~dp0.nuget\NuGet.config"
echo     ^</AspNetVNext^> >>"%~dp0.nuget\NuGet.config"
echo   ^</packageSourceCredentials^> >>"%~dp0.nuget\NuGet.config"
echo ^</configuration^> >>"%~dp0.nuget\NuGet.config"
)

IF "%2"=="" (
    echo Finding latest version
    FOR /f "tokens=1,2" %%G in ('"%~dp0.nuget\NuGet.exe" list ProjectK -Prerelease -ConfigFile %~dp0.nuget\NuGet.config') DO (
      IF "%%G"=="ProjectK" (
        SET _KVM_VERSION=%%H
      )
    )
) ELSE (
    SET "_KVM_VERSION=%2"
)

ECHO Downloading version %_KVM_VERSION%
"%~dp0.nuget\NuGet.exe" install ProjectK -Version %_KVM_VERSION% -OutputDirectory "%~dp0packages" -ConfigFile "%~dp0.nuget\NuGet.config"

CALL:cmd_use use %_KVM_VERSION%
GOTO:EOF


:cmd_use
IF NOT EXIST "%~dp0k.cmd" (
  ECHO @CALL %%~dp0kvm.cmd k %%* >%~dp0k.cmd
)
IF EXIST "%~dp0alias\%2.txt" (
  FOR /F %%G IN (%~dp0alias\%2.txt) DO (
    ECHO Setting _KVM_VERSION to '%%G'
    SET "_KVM_VERSION=%%G"
  )
) ELSE (
  IF NOT EXIST "%~dp0packages\ProjectK.%2\tools\k.cmd" (
    ECHO Version '%2' not found. 
    ECHO You may need to run 'kvm install %2' 
    GOTO:EOF
  )
  ECHO Setting _KVM_VERSION to '%2'
  SET "_KVM_VERSION=%2"
)
GOTO:EOF


:cmd_alias
IF NOT EXIST "%~dp0alias" (
  MKDIR "%~dp0alias"
)
IF "%3"=="" (
  IF "%2"=="" (
    DIR "%~dp0alias" /b
  ) ELSE (
    ECHO Alias '%2' is
    TYPE "%~dp0alias\%2.txt"
  )
) ELSE (
  IF NOT EXIST "%~dp0packages\ProjectK.%3\tools\k.cmd" (
    ECHO Version '%3' not found. 
    ECHO You may need to run 'kvm install %3' 
    GOTO:EOF
  )

  ECHO Setting alias '%2' to '%3'
  ECHO %3>%~dp0alias\%2.txt
)
GOTO:EOF


:cmd_list
dir /b "%~dp0packages\ProjectK*"
GOTO:EOF


:cmd_k
@REM find k.cmd in local paths

@REM read _KVM_VERSION.txt if _KVM_VERSION not set
IF "%_KVM_VERSION%" == "" (
  FOR /F %%G IN (%~dp0alias\default.txt) DO (
    SET "_KVM_VERSION=%%G"
  )
)
IF NOT EXIST "%~dp0packages\ProjectK.%_KVM_VERSION%\tools\k.cmd" (
  ECHO Version '%_KVM_VERSION%' not found. 
  ECHO You may need to run 'kvm install %_KVM_VERSION%' 
) ELSE (
  CALL "%~dp0packages\ProjectK.%_KVM_VERSION%\tools\k.cmd" %2 %3 %4 %5 %6 %7 %8 %9
)
GOTO:EOF


:cmd_help
ECHO kvm ^<command^> [args...]
ECHO   k version manager
ECHO .
ECHO kvm help
ECHO   displays this help
ECHO .
ECHO kvm upgrade
ECHO   install latest k version and make it the default
ECHO .
ECHO kvm install ^<version^>
ECHO   install and use specific k version
ECHO .
ECHO kvm list
ECHO   list installed k versions
ECHO .
ECHO kvm use ^<version^>^|^<alias^>
ECHO   use a version or alias within the current command prompt
ECHO .
ECHO kvm alias ^<alias^> ^<version^>
ECHO   create alias to a specific version
ECHO   alias names may be passed to 'kvm use ^<alias^>'
ECHO   the alias 'default' determines the default k version
ECHO   when kvm use is not called
ECHO .
ECHO kvm alias ^<alias^>
ECHO   show the version of an alias
ECHO .
ECHO kvm alias
ECHO   list aliases
ECHO .

GOTO:EOF


