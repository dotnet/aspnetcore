@ECHO OFF
SET RepoRoot=%~dp0..\..\..
%RepoRoot%\eng\build.cmd -projects %~dp0**\*.csproj /p:SkipIISNewHandlerTests=true /p:SkipIISNewShimTests=true %*
