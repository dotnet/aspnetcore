param(
    [switch]$ci
)

$ErrorActionPreference = 'stop'
$excludeCIBinarylog = $true
$msbuildEngine = 'dotnet'
$repoRoot = Resolve-Path "$PSScriptRoot/../.."

try {
  & "$repoRoot\eng\common\msbuild.ps1" -msbuildEngine $msbuildEngine -ci:$ci "$repoRoot/eng/CodeGen.proj" /t:GenerateProjectList
} finally {
  Remove-Item variable:global:_BuildTool -ErrorAction Ignore
  Remove-Item variable:global:_DotNetInstallDir -ErrorAction Ignore
  Remove-Item variable:global:_ToolsetBuildProj -ErrorAction Ignore
  Remove-Item variable:global:_MSBuildExe -ErrorAction Ignore
}
