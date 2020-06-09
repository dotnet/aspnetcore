param(
    [switch]$ci
)

$ErrorActionPreference = 'stop'
$repoRoot = Resolve-Path "$PSScriptRoot/../.."

try {
  # eng/common/msbuild.ps1 builds the Debug configuration unless there's a $Configuration variable. Match that here.
  & "$repoRoot\build.ps1"  -ci:$ci -nobl -noBuildRepoTasks -noRestore -buildNative -configuration Debug

  Remove-Item variable:global:_BuildTool -ErrorAction Ignore

  $excludeCIBinarylog = $true
  $msbuildEngine = 'dotnet'
  & "$repoRoot\eng\common\msbuild.ps1" -ci:$ci "$repoRoot/eng/CodeGen.proj" /t:GenerateReferenceSources
} finally {
  Remove-Item variable:global:_BuildTool -ErrorAction Ignore
  Remove-Item variable:global:_DotNetInstallDir -ErrorAction Ignore
  Remove-Item variable:global:_ToolsetBuildProj -ErrorAction Ignore
  Remove-Item variable:global:_MSBuildExe -ErrorAction Ignore
}
