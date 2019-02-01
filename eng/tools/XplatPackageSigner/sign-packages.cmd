@ECHO OFF

SET DirToSign=%1

IF "%DirToSign%"=="" (
    echo Error^: Expected argument ^<DirToSign^>
    echo Usage^: sign-packages.cmd ^<DirToSign^>

    exit /b 1
)

SET RepoRoot=%~dp0..\..\..
SET Project=%~dp0\XplatPackageSigner.proj

%RepoRoot%\build.cmd -NoRestore -projects %project% /p:DirectoryToSign=%DirToSign% /bl:%RepoRoot%\artifacts\logs\XplatSign.binlog
