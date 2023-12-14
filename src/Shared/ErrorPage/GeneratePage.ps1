$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$ToolPath = Join-Path "$RepoRoot" "artifacts\bin\RazorPageGenerator\Debug\net9.0\dotnet-razorpagegenerator.exe"

if (!(Test-Path "$ToolPath")) {
    throw "Unable to find razor page generator tool at $ToolPath"
}

& "$ToolPath" Microsoft.AspNetCore.Hosting.Views "$PSScriptRoot" Views/

$TargetPath = Join-Path "$PSScriptRoot" "ErrorPage.Designer.cs"
if (Test-Path "$TargetPath") {
    Remove-Item "$TargetPath"
}

Move-Item "$PSScriptRoot\Views\ErrorPage.Designer.cs" "$PSScriptRoot"
