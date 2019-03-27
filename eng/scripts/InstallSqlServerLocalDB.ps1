<#
.SYNOPSIS
    Installs SQL Server 2016 Express LocalDB on a machine.
.DESCRIPTION
    This script installs Microsoft SQL Server 2016 Express LocalDB on a machine.
.LINK
    https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-2016-express-localdb?view=sql-server-2016
    https://docs.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server-from-the-command-prompt?view=sql-server-2016
#>

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

Write-Host "Installing SQL Server 2016 Express LocalDB" -f Magenta

# Download SqlLocalDB.msi.
$installerFilename = "SqlLocalDB.msi"
$installerPath = "$intermedateDir\$installerFilename"
Write-Host ""
Write-Host "Downloading '$installerFilename' to '$installerPath'."
Invoke-WebRequest -OutFile $installerPath -Uri `
  "https://download.microsoft.com/download/9/0/7/907AD35F-9F9C-43A5-9789-52470555DB90/ENU/SqlLocalDB.msi"

# Install LocalDB.
$arguments = '/package', "`"$installerPath`"", '/NoRestart', '/Passive', `
  'IACCEPTSQLLOCALDBLICENSETERMS=YES', 'HIDEPROGRESSBAR=YES'
Write-Host ""
Write-Host "Running 'msiexec $arguments'."
$process = Start-Process msiexec.exe -ArgumentList $arguments -NoNewWindow -PassThru -Verbose -Wait
exit $process.ExitCode
