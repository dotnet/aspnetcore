# Takes an input browser log file and splits it into separate files for each browser
param(
    [Parameter(Mandatory = $true, Position = 0)][string]$InputFile,
    [Parameter(Mandatory = $false)][string]$OutputDirectory
)

if (!$OutputDirectory) {
    $OutputDirectory = Split-Path -Parent $InputFile
}

$browserParser = [regex]"(?<name>[a-zA-Z]*) (?<version>[^ ]*) \((?<os>[^\)]*)\)";
Write-Host "Processing log file...";
$browsers = @{}
Get-Content $InputFile | ForEach-Object {
    $openSquare = $_.IndexOf("[");
    $closeSquare = $_.IndexOf("]");
    if (($openSquare -ge 0) -and ($closeSquare -ge 0)) {
        $browser = $_.Substring($openSquare + 1, $closeSquare - 1);
        $message = $_.Substring($closeSquare + 1).Trim();

        # Parse the browser
        $m = $browserParser.Match($browser)
        if ($m.Success) {
            $name = $m.Groups["name"].Value;
            $version = $m.Groups["version"].Value;
            $os = $m.Groups["os"].Value;

            # Generate a new file name
            $fileName = "$($name)_$($version.Replace(".", "_")).log"
            $lines = $browsers[$fileName]
            if (!$lines) {
                $lines = @();
            }

            $browsers[$fileName] = $lines + $message
        }
    }
}

$browsers.Keys | ForEach-Object {
    Write-Host "Writing to $_ ..."
    $destination = Join-Path $OutputDirectory $_
    [IO.File]::WriteAllText($destination, [string]::Join([Environment]::NewLine, $browsers[$_]))
}