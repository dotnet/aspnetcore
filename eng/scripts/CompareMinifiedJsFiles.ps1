[string[]] $errors = @()

function _compareFiles($fileName) {
    Write-Host "Comparing contents for $fileName"
    $repoRoot = Resolve-Path "$PSScriptRoot/../.."
    $localFile = "$repoRoot/src/Components/Web.JS/dist/Release/$fileName"
    $submoduleFile = "$repoRoot/src/submodules/BlazorMinifiedJs/src/$fileName"
    $delta = diff -w $localFile $submoduleFile
    if (![string]::IsNullOrEmpty($delta)) {
        $script:errors += "Diff found in $fileName, please push new minifed .js files to https://github.com/dotnet/BlazorMinifiedJs/tree/main/src"
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