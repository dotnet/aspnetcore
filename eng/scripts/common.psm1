$ErrorActionPreference = 'Stop'
# Update the default TLS support to 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Invoke-Block([scriptblock]$cmd, [string]$WorkingDir = $null) {
    if ($WorkingDir) {
        Push-Location $WorkingDir
    }

    try {

        $cmd | Out-String | Write-Verbose
        & $cmd

        # Need to check both of these cases for errors as they represent different items
        # - $?: did the powershell script block throw an error
        # - $lastexitcode: did a windows command executed by the script block end in error
        if ((-not $?) -or ($lastexitcode -ne 0)) {
            if ($error -ne $null)
            {
                Write-Warning $error[0]
            }
            throw "Command failed to execute: $cmd"
        }
    }
    finally {
        if ($WorkingDir) {
            Pop-Location
        }
    }
}

function SaveXml([xml]$xml, [string]$path) {
    Write-Verbose "Saving to $path"
    $ErrorActionPreference = 'stop'

    $settings = New-Object System.XML.XmlWriterSettings
    $settings.OmitXmlDeclaration = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding( $true )
    $writer = [System.XML.XMLTextWriter]::Create($path, $settings)
    $xml.Save($writer)
    $writer.Close()
}

function LoadXml([string]$path) {
    Write-Verbose "Reading from $path"

    $ErrorActionPreference = 'stop'
    $obj = new-object xml
    $obj.PreserveWhitespace = $true
    $obj.Load($path)
    return $obj
}

function Get-RemoteFile([string]$RemotePath, [string]$LocalPath) {
    if ($RemotePath -notlike 'http*') {
        Copy-Item $RemotePath $LocalPath
        return
    }

    $retries = 10
    while ($retries -gt 0) {
        $retries -= 1
        try {
            $ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
            Invoke-WebRequest -UseBasicParsing -Uri $RemotePath -OutFile $LocalPath
            return
        }
        catch {
            Write-Verbose "Request failed. $retries retries remaining"
        }
    }

    Write-Error "Download failed: '$RemotePath'."
}
