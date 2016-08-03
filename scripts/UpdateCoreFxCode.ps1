param([string]$CoreFxRepoRoot)

$RepoRoot = Split-Path -Parent $PSScriptRoot

$FilesToCopy = @(
    "src\System.Net.WebSockets.Client\src\System\Net\WebSockets\ManagedWebSocket.cs",
    "src\Common\src\System\Net\WebSockets\WebSocketValidate.cs"
)

if(!$CoreFxRepoRoot) {
    $CoreFxRepoRoot = "$RepoRoot\..\..\dotnet\corefx"
}

if(!(Test-Path $CoreFxRepoRoot)) {
    throw "Could not find CoreFx repo at $CoreFxRepoRoot"
}
$CoreFxRepoRoot = Convert-Path $CoreFxRepoRoot

$DestinationRoot = "$RepoRoot\src\Microsoft.AspNetCore.WebSockets.Protocol\ext"

$FilesToCopy | foreach {
    $Source = Join-Path $CoreFxRepoRoot $_
    $Destination = Join-Path $DestinationRoot $_
    $DestinationDir = Split-Path -Parent $Destination

    if(!(Test-Path $Source)) {
        Write-Warning "Can't find source file: $Source"
    } else {
        if(!(Test-Path $DestinationDir)) {
            mkdir $DestinationDir | Out-Null
        }
        if(Test-Path $Destination) {
            del $Destination
        }
        Write-Host "Copying $_"

        $SourceCode = [IO.File]::ReadAllText($Source)
        $SourceCode = $SourceCode.Replace("Task.FromException", "CompatHelpers.FromException")
        $SourceCode = $SourceCode.Replace("Task.CompletedTask", "CompatHelpers.CompletedTask")
        $SourceCode = $SourceCode.Replace("Array.Empty", "CompatHelpers.Empty")
        $SourceCode = $SourceCode.Replace("nameof(ClientWebSocket)", "`"ClientWebSocket`"")
        [IO.File]::WriteAllText($Destination, $SourceCode)
    }
}