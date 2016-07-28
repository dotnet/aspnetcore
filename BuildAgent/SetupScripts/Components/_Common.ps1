$setupFilesShare = $env:ASPNETCI_SETUP_FILES_SHARE
if(!$setupFilesShare) {
    $setupFilesShare = "\\aspnetci\share\BuildAgentSetupFiles"
}

function With-SupportFile([string]$RelativePath, [scriptblock]$Action) {
    $Name = Split-Path -Leaf $RelativePath
    try {
        Copy "$setupFilesShare\$RelativePath" "c:\$Name"
        $Action.Invoke()
    } catch {
        throw $error[1]
    } finally {
        if(Test-Path "c:\$Name") {
            Del "c:\$Name" -Force
        }
    }
}

function Ensure-Path($value) {
    function _ensurePathInVariable($current) {
        $values = $current.Split(";")
        if($values -icontains $value) {
            "$current"
        } else {
            "$value;$current"
        }
    }

    $env:PATH = _ensurePathInVariable $env:PATH
    [Environment]::SetEnvironmentVariable("PATH", (_ensurePathInVariable ([Environment]::GetEnvironmentVariable("PATH", "Machine"))), "Machine")
}

function Ensure-Msi($Name, $Id, $RelativePath) {
    $FileName = Split-Path -Leaf $RelativePath
    if($Id -and (Get-WmiObject -Query "SELECT * FROM Win32_Product WHERE IdentifyingNumber = '$Id'")) {
        Write-Host "`n$Name already installed"
    } else {
        Write-Host "`nInstalling $Name..."
        if(!(Test-Path "C:\MsiLogs")) {
            mkdir "C:\MsiLogs" | Out-Null
        }

        With-SupportFile $RelativePath {
            $args="/i c:\$FileName /qn /l* C:\MsiLogs\$FileName.log"
            Start-Process -FilePath msiexec.exe -ArgumentList $args -Wait
        }
    }
}

