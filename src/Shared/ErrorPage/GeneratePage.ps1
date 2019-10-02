param(
    [Parameter(Mandatory = $true)][string]$ToolingRepoPath
)

$ToolPath = Join-Path $ToolingRepoPath "artifacts\bin\RazorPageGenerator\Debug\netcoreapp5.0\dotnet-razorpagegenerator.exe"

if (!(Test-Path $ToolPath)) {
    throw "Unable to find razor page generator tool at $ToolPath"
}

& "$ToolPath" Microsoft.AspNetCore.Hosting.Views $PSScriptRoot

$TargetPath = Join-Path $PSScriptRoot "ErrorPage.Designer.cs"
if (Test-Path $TargetPath) {
    Remove-Item $TargetPath
}

Move-Item "$PSScriptRoot\Views\ErrorPage.Designer.cs" $PSScriptRoot