@rem
@rem Copyright 2015 the original author or authors.
@rem
@rem Licensed under the Apache License, Version 2.0 (the "License");
@rem you may not use this file except in compliance with the License.
@rem You may obtain a copy of the License at
@rem
@rem      https://www.apache.org/licenses/LICENSE-2.0
@rem
@rem Unless required by applicable law or agreed to in writing, software
@rem distributed under the License is distributed on an "AS IS" BASIS,
@rem WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
@rem See the License for the specific language governing permissions and
@rem limitations under the License.
@rem
@rem SPDX-License-Identifier: Apache-2.0
@rem

@if "%DEBUG%"=="" @echo off
@rem ##########################################################################
@rem
@rem  Gradle startup script for Windows
@rem
@rem ##########################################################################

@rem Set local scope for the variables with windows NT shell
if "%OS%"=="Windows_NT" setlocal

set DIRNAME=%~dp0
if "%DIRNAME%"=="" set DIRNAME=.
@rem This is normally unused
set APP_BASE_NAME=%~n0
set APP_HOME=%DIRNAME%

@rem Resolve any "." and ".." in APP_HOME to make it shorter.
for %%i in ("%APP_HOME%") do set APP_HOME=%%~fi

@rem Add default JVM options here. You can also use JAVA_OPTS and GRADLE_OPTS to pass JVM options to this script.
set DEFAULT_JVM_OPTS="-Xmx64m" "-Xms64m"

@rem Find java.exe
if defined JAVA_HOME goto findJavaFromJavaHome

set JAVA_EXE=java.exe
%JAVA_EXE% -version >NUL 2>&1
if %ERRORLEVEL% equ 0 goto execute

echo. 1>&2
echo ERROR: JAVA_HOME is not set and no 'java' command could be found in your PATH. 1>&2
echo. 1>&2
echo Please set the JAVA_HOME variable in your environment to match the 1>&2
echo location of your Java installation. 1>&2

goto fail

:findJavaFromJavaHome
set JAVA_HOME=%JAVA_HOME:"=%
set JAVA_EXE=%JAVA_HOME%/bin/java.exe

if exist "%JAVA_EXE%" goto execute

echo. 1>&2
echo ERROR: JAVA_HOME is set to an invalid directory: %JAVA_HOME% 1>&2
echo. 1>&2
echo Please set the JAVA_HOME variable in your environment to match the 1>&2
echo location of your Java installation. 1>&2

goto fail

:execute
@rem Setup the command line


set BOOTSTRAP_ATTEMPT=1
set BOOTSTRAP_MAX_ATTEMPTS=3
set BOOTSTRAP_OUTPUT=%TEMP%\gradle-bootstrap-%RANDOM%%RANDOM%.log
for /f "tokens=1,* delims==" %%i in ('findstr /b "distributionUrl=" "%APP_HOME%\gradle\wrapper\gradle-wrapper.properties"') do set DISTRIBUTION_URL=%%j
for /f "tokens=2 delims=/" %%i in ("%DISTRIBUTION_URL%") do set DISTRIBUTION_ENDPOINT=%%i
for /f "tokens=1,2 delims=@" %%i in ("%DISTRIBUTION_ENDPOINT%") do if not "%%j"=="" set DISTRIBUTION_ENDPOINT=%%j

:bootstrap
echo Gradle distribution bootstrap attempt %BOOTSTRAP_ATTEMPT% of %BOOTSTRAP_MAX_ATTEMPTS%: %DISTRIBUTION_ENDPOINT%
call :runBootstrap
set BOOTSTRAP_EXIT_CODE=%ERRORLEVEL%
if %BOOTSTRAP_EXIT_CODE% equ 0 goto executeGradle

findstr /i /c:"UnknownHostException" "%BOOTSTRAP_OUTPUT%" >NUL
if not errorlevel 1 (
    set "BOOTSTRAP_REASON=hostname resolution failure"
    goto bootstrapTransientFailure
)
findstr /i /c:"ConnectException" /c:"SocketTimeoutException" /c:"Connection reset" /c:"Connection timed out" /c:"Read timed out" /c:"Premature EOF" "%BOOTSTRAP_OUTPUT%" >NUL
if not errorlevel 1 (
    set "BOOTSTRAP_REASON=transient connection failure"
    goto bootstrapTransientFailure
)
findstr /i /r /c:"HTTP response code: 408" /c:"HTTP response code: 429" /c:"HTTP response code: 5[0-9][0-9]" "%BOOTSTRAP_OUTPUT%" >NUL
if not errorlevel 1 (
    set "BOOTSTRAP_REASON=transient HTTP response"
    goto bootstrapTransientFailure
)
findstr /i /c:"Verification of Gradle distribution failed" /c:"does not match the expected checksum" /c:"distribution SHA-256 sum" "%BOOTSTRAP_OUTPUT%" >NUL
if not errorlevel 1 (
    goto bootstrapChecksumFailure
)
goto bootstrapFail

:bootstrapTransientFailure
if %BOOTSTRAP_ATTEMPT% geq %BOOTSTRAP_MAX_ATTEMPTS% goto bootstrapRetryExhausted
if %BOOTSTRAP_ATTEMPT% equ 1 (set BOOTSTRAP_DELAY=2) else (set BOOTSTRAP_DELAY=5)
echo %BOOTSTRAP_REASON% acquiring Gradle distribution from %DISTRIBUTION_ENDPOINT%; retrying in %BOOTSTRAP_DELAY% seconds.
timeout /t %BOOTSTRAP_DELAY% /nobreak >NUL
set /a BOOTSTRAP_ATTEMPT+=1
goto bootstrap

:bootstrapChecksumFailure
del "%BOOTSTRAP_OUTPUT%" >NUL 2>&1
echo Gradle distribution bootstrap checksum verification failed. 1>&2
goto fail

:bootstrapFail
del "%BOOTSTRAP_OUTPUT%" >NUL 2>&1
echo Gradle distribution bootstrap failed with a non-transient error. 1>&2
goto fail

:bootstrapRetryExhausted
del "%BOOTSTRAP_OUTPUT%" >NUL 2>&1
echo Gradle distribution bootstrap failed after %BOOTSTRAP_MAX_ATTEMPTS% attempts due to transient network errors. 1>&2
goto fail

:runBootstrap
"%JAVA_EXE%" %DEFAULT_JVM_OPTS% %JAVA_OPTS% %GRADLE_OPTS% "-Dorg.gradle.appname=%APP_BASE_NAME%" -jar "%APP_HOME%\gradle\wrapper\gradle-wrapper.jar" --version > "%BOOTSTRAP_OUTPUT%" 2>&1
exit /b %ERRORLEVEL%

:executeGradle
del "%BOOTSTRAP_OUTPUT%" >NUL 2>&1

@rem Execute Gradle
"%JAVA_EXE%" %DEFAULT_JVM_OPTS% %JAVA_OPTS% %GRADLE_OPTS% "-Dorg.gradle.appname=%APP_BASE_NAME%" -jar "%APP_HOME%\gradle\wrapper\gradle-wrapper.jar" %*

:end
@rem End local scope for the variables with windows NT shell
if %ERRORLEVEL% equ 0 goto mainEnd

:fail
rem Set variable GRADLE_EXIT_CONSOLE if you need the _script_ return code instead of
rem the _cmd.exe /c_ return code!
set EXIT_CODE=%ERRORLEVEL%
if %EXIT_CODE% equ 0 set EXIT_CODE=1
if not ""=="%GRADLE_EXIT_CONSOLE%" exit %EXIT_CODE%
exit /b %EXIT_CODE%

:mainEnd
if "%OS%"=="Windows_NT" endlocal

:omega
