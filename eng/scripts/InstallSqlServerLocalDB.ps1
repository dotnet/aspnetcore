<#
.SYNOPSIS
    Installs SQL Server 2017 Express LocalDB on a machine.
.DESCRIPTION
    This script installs Microsoft SQL Server 2016 Express LocalDB on a machine.
.LINK
    https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-2016-express-localdb?view=sql-server-2017
    https://docs.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server-from-the-command-prompt?view=sql-server-2017
#>

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 1

$intermedateDir = "$PSScriptRoot\obj"
mkdir $intermedateDir -ErrorAction Ignore | Out-Null

$bootstrapper = "$intermedateDir\SQLExpressInstaller.exe"
$ProgressPreference = 'SilentlyContinue' # Workaround PowerShell/PowerShell#2138

Write-Host "Installing SQL Server 2017 Express LocalDB" -f Magenta

# Get the bootstrapper.
Write-Host ""
Write-Host "Downloading 'SQLServer2017-SSEI-Expr.exe' to '$bootstrapper'."
Invoke-WebRequest -OutFile $bootstrapper -Uri `
  "https://download.microsoft.com/download/5/E/9/5E9B18CC-8FD5-467E-B5BF-BADE39C51F73/SQLServer2017-SSEI-Expr.exe"

# Download SqlLocalDB.msi.
Write-Host ""
$arguments = '/Action=Download', '/Quiet', '/HideProgressBar', `
  '/MediaType=LocalDB', "/MediaPath=`"$intermedateDir`"", '/Language=en-us'
Write-Host "Running '`"$bootstrapper`" $arguments'."
$process = Start-Process "$bootstrapper" -ArgumentList $arguments -PassThru -Verbose -Wait
if ($process.ExitCode -ne 0) {
  exit $process.ExitCode
}

# Install LocalDB.
Write-Host ""
$arguments = '/package', "`"$intermedateDir\en-us\SqlLocalDB.msi`"", '/NoRestart', '/Passive', `
  'IACCEPTSQLLOCALDBLICENSETERMS=YES', 'HIDEPROGRESSBAR=YES'
Write-Host "Running 'msiexec $arguments'."
$process = Start-Process msiexec.exe -ArgumentList $arguments -NoNewWindow -PassThru -Verbose -Wait
exit $process.ExitCode
