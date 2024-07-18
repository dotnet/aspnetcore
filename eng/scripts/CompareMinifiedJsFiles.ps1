[string[]] $errors = @()

function _compareFiles($fileName) {
    Write-Host "Comparing contents for $fileName"
    $repoRoot = Resolve-Path "$PSScriptRoot/../.."
    $localFile = "$repoRoot/src/Components/Web.JS/dist/Release/$fileName"
    $submoduleFile = "$repoRoot/src/submodules/BlazorMinifiedJs/src/$fileName"
    $delta = Compare-Object -ReferenceObject ((Get-Content -Path $submoduleFile).trim()) -DifferenceObject ((Get-Content -Path $localFile).trim())
    if (![string]::IsNullOrEmpty($delta)) {
        $script:errors += "Diff found in $fileName, please see https://github.com/dotnet/aspnetcore/blob/main/docs/UpdatingMinifiedJsFiles.md for remediation steps"
    }
}

$MinifiedJsFiles = "blazor.web.js","blazor.server.js","blazor.webview.js"

foreach ($JsFile in $MinifiedJsFiles) {
    _compareFiles -fileName $JsFile
}

foreach ($err in $errors) {
    Write-Host -f Red $err
}

if ($errors) {
    exit 1
}