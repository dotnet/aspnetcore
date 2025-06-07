@ECHO OFF
SET RepoRoot=%~dp0..\..

call %RepoRoot%\eng\build.cmd -projects %RepoRoot%\src\Validation\**\*.csproj %*