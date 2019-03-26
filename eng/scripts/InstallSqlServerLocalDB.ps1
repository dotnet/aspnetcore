<#
.SYNOPSIS
    Installs SQL Server 2017 Express LocalDB on a machine.
.DESCRIPTION
    This script installs Microsoft SQL Server 2017 Express LocalDB on a machine.
.LINK
    https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-2016-express-localdb?view=sql-server-2017
    https://docs.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server-from-the-command-prompt?view=sql-server-2017
#>

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

Write-Host "Installing SQL Server 2017 Express LocalDB" -f Magenta

# Download SqlLocalDB.msi.
$installerFilename = "SqlLocalDB.msi"
$installerPath = "$intermedateDir\$installerFilename"
Write-Host ""
Write-Host "Downloading '$installerFilename' to '$installerPath'."
Invoke-WebRequest -OutFile $installerPath -Uri `
  "https://download.microsoft.com/download/E/F/2/EF23C21D-7860-4F05-88CE-39AA114B014B/$installerFilename"

# Install LocalDB.
$arguments = '/package', "`"$installerPath`"", '/NoRestart', '/Passive', `
  'IACCEPTSQLLOCALDBLICENSETERMS=YES', 'HIDEPROGRESSBAR=YES'
Write-Host ""
Write-Host "Running 'msiexec $arguments'."
$process = Start-Process msiexec.exe -ArgumentList $arguments -NoNewWindow -PassThru -Verbose -Wait
if ($process.ExitCode -ne 0)
{
  exit $process.ExitCode
}

# Download SQLServer2017-KB4484710-x64.exe.
$installerFilename = "SQLServer2017-KB4484710-x64.exe"
$installerPath = "$intermedateDir\$installerFilename"
Write-Host ""
Write-Host "Downloading SQL Server 2017 Cumulative Update 14 to '$installerPath'."
Invoke-WebRequest -OutFile $installerPath -Uri `
  "https://download.microsoft.com/download/C/4/F/C4F908C9-98ED-4E5F-88D5-7D6A5004AEBD/$installerFilename"

# Update LocalDB.
$arguments = '/Action=Patch', '/AllInstances', '/IAcceptSQLServerLicenseTerms', `
  '/Quiet', '/SuppressPrivacyStatementNotice'
Write-Host ""
Write-Host "Running '`"$installerPath`" $arguments'."
$process = Start-Process "$installerPath" -ArgumentList $arguments -PassThru -Verbose -Wait
exit $process.ExitCode
