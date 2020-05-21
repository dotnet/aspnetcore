param(
    [switch]$ci
)

$ErrorActionPreference = 'stop'
$msbuildEngine = 'dotnet'
$repoRoot = Resolve-Path "$PSScriptRoot/../.."

try {
  & "$repoRoot\eng\common\msbuild.ps1" -ci:$ci "$repoRoot/eng/CodeGen.proj" `
    /t:GenerateProjectList `
    /bl:artifacts/log/genprojlist.binlog
} finally {
  Remove-Item variable:global:_BuildTool -ErrorAction Ignore
  Remove-Item variable:global:_DotNetInstallDir -ErrorAction Ignore
  Remove-Item variable:global:_ToolsetBuildProj -ErrorAction Ignore
  Remove-Item variable:global:_MSBuildExe -ErrorAction Ignore
}
