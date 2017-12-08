<#
.SYNOPSIS
    Installs asnetcore to IISExpress and IIS directory
.DESCRIPTION
    Installs asnetcore to IISExpress and IIS directory
.PARAMETER Rollback
    Default: $false
    Rollback the updated files with the original files
.PARAMETER ForceToBackup
    Default: $false
    Force to do the initial backup again (this parameter is meaningful only when you want to replace the existing backup file)
.PARAMETER Extract
    Default: $false
    Search ANCM nugetfile and extract the file to the path of the ExtractFilesTo parameter value
.PARAMETER PackagePath
    Default: $PSScriptRoot\..\..\artifacts
    Root path where ANCM nuget package is placed
.PARAMETER ExtractFilesTo
    Default: $PSScriptRoot\..\..\artifacts"
    Output path where aspentcore.dll file is extracted

Example:
    .\installancm.ps1 "C:\Users\jhkim\AppData\Local\Temp\ihvufnf1.atw\ancm\Debug"

#>
[cmdletbinding()]
param(
   [Parameter(Mandatory=$false, Position = 0)]
   [string]  $ExtractFilesTo="$PSScriptRoot\..\artifacts\build\AspNetCore\bin\Debug",
   [Parameter(Mandatory=$false, Position = 1)]
   [string]  $PackagePath="$PSScriptRoot\..\artifacts\build",
   [Parameter(Mandatory=$false)]
   [switch] $Rollback=$false,
   [Parameter(Mandatory=$false)]
   [switch] $ForceToBackup=$false,
   [Parameter(Mandatory=$false)]
   [switch] $Extract=$false
)

function Get-ANCMNugetFilePath() { 

    $NugetFilePath = Get-ChildItem $PackagePath -Recurse -Filter Microsoft.AspNetCore.AspNetCoreModule*.nupkg | Select-Object -Last 1
    return ($NugetFilePath.FullName)
}

function Check-TargetFiles() { 
    $functionName = "Check-TargetFiles"
    $LogHeader = "[$ScriptFileName::$functionName]"
    $result = $true

    if (-not $isIISExpressInstalled -and -not $isIISInstalled)
    {
        Say ("$LogHeader Both IIS and IISExpress does not have aspnetcore.dll file")
        $result = $false
    }

    if ($isIISExpressInstalled)
    {
        if (-not (Test-Path $aspnetCorex64To))
        {
            Say ("$LogHeader Error!!! Failed to find the file $aspnetCorex64To")
            $result = $false
        }
        if (-not (Test-Path $aspnetCoreSchemax64To))
        {
            Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreSchemax64To")
            $result = $false
        }
        if ($is64BitMachine)
        {
            if (-not (Test-Path $aspnetCoreWin32To))
            {
                Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreWin32To")
                $result = $false
            }    
            if (-not (Test-Path $aspnetCoreSchemaWin32To))
            {
                Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreSchemaWin32To")
                $result = $false
            }  
        }
    }

    if ($isIISInstalled)
    {
        if (-not (Test-Path $aspnetCorex64IISTo))
        {
            Say ("$LogHeader Error!!! Failed to find the file $aspnetCorex64IISTo")
            $result = $false
        }
        if (-not (Test-Path $aspnetCoreSchemax64IISTo))
        {
            Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreSchemax64IISTo")
            $result = $false
        }
        if ($is64BitMachine)
        {
            if (-not (Test-Path $aspnetCoreWin32IISTo))
            {
                Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreWin32IISTo")
                $result = $false
            }
        }
    }

    return $result
}

function Check-ExtractedFiles() { 
    $functionName = "Check-ExtractedFiles"
    $LogHeader = "[$ScriptFileName::$functionName]"
    $result = $true

    if (-not (Test-Path $aspnetCorex64From))
    {
        Say ("$LogHeader Error!!! Failed to find the file $aspnetCorex64From")
        $result = $false
    }
    if (-not (Test-Path $aspnetCoreWin32From))
    {
        Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreWin32From")
        $result = $false
    }
    if (-not (Test-Path $aspnetCoreSchemax64From))
    {
        Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreSchemax64From")
        $result = $false
    }
    if (-not (Test-Path $aspnetCoreSchemaWin32From))
    {
        Say ("$LogHeader Error!!! Failed to find the file $aspnetCoreSchemaWin32From")
        $result = $false
    }
    return $result
}

function Extract-ANCMFromNugetPackage() { 
    $result = $true

    $functionName = "Extract-ANCMFromNugetPackage"
    $LogHeader = "[$ScriptFileName::$functionName]"

    $backupAncmNugetFilePath = Join-Path $TempExtractFilesTo (get-item $ancmNugetFilePath).Name
    if (Test-Path $backupAncmNugetFilePath)
    {
        Say ("$LogHeader Found backup file at $backupAncmNugetFilePath")
        if ((get-item $ancmNugetFilePath).LastWriteTime -eq (get-item $backupAncmNugetFilePath).LastWriteTime)
        {
            if (Check-ExtractedFiles)
            {
                Say ("$LogHeader Skip to extract ANCM files because $ancmNugetFilePath is matched to the backup file $backupAncmNugetFilePath.")
                return $result
            }
        }
    }

    Add-Type -Assembly System.IO.Compression.FileSystem
    if (Test-Path $TempExtractFilesTo)
    {
        remove-item $TempExtractFilesTo -Force -Recurse -Confirm:$false | out-null
    }
    if (Test-Path $TempExtractFilesTo)
    {
        Say ("$LogHeader Error!!! Failed to delete $TempExtractFilesTo")
        $result = $false
        return $result
    }
    else
    {
        new-item -Type directory $TempExtractFilesTo | out-null
    }
    if (-not (Test-Path $TempExtractFilesTo))
    {
        Say ("$LogHeader Error!!! Failed to create $TempExtractFilesTo")
        $result = $false
        return $result
    }

    # 
    Say ("$LogHeader Extract the ancm nuget file $ancmNugetFilePath to $TempExtractFilesTo ...")
    [System.IO.Compression.ZipFile]::ExtractToDirectory($ancmNugetFilePath, $TempExtractFilesTo) 

    Say ("$LogHeader Create the backup file of the nuget file to $backupAncmNugetFilePath")
    copy-item $ancmNugetFilePath $backupAncmNugetFilePath

    return $result
}

function Update-ANCM() { 
    
    $functionName = "Update-ANCM -Rollback:$" + $Rollback.ToString()
    $LogHeader = "[$ScriptFileName::$functionName]"

    if ($isIISExpressInstalled)
    {
        if ($is64BitMachine)
        {
            Say ("$LogHeader Start updating ANCM files for IISExpress for amd64 machine...")
            Update-File $aspnetCorex64From $aspnetCorex64To 
            Update-File $aspnetCoreWin32From $aspnetCoreWin32To 
            Update-File $aspnetCoreSchemax64From $aspnetCoreSchemax64To 
            Update-File $aspnetCoreSchemaWin32From $aspnetCoreSchemaWin32To 
        }
        else
        {
            Say ("$LogHeader Start updating ANCM files for IISExpress for x86 machine...")
            Update-File $aspnetCoreWin32From $aspnetCorex64To 
            Update-File $aspnetCoreSchemaWin32From $aspnetCoreSchemax64To 
        }
    }
    else
    {
        Say ("$LogHeader Can't find aspnetcore.dll for IISExpress. Skipping updating ANCM files for IISExpress")
    }

    if ($isIISInstalled)
    {
        if ($is64BitMachine)
        {
            Say ("$LogHeader Start updating ANCM files for IIS for amd64 machine...")
            Update-File $aspnetCorex64From $aspnetCorex64IISTo 
            Update-File $aspnetCoreWin32From $aspnetCoreWin32IISTo 
            Update-File $aspnetCoreSchemax64From $aspnetCoreSchemax64IISTo 
        }
        else
        {
            Say ("$LogHeader Start updating ANCM files for IIS for x86 machine...")
            Update-File $aspnetCoreWin32IISFrom $aspnetCorex64IISTo 
            Update-File $aspnetCoreSchemaWin32From $aspnetCoreSchemax64IISTo 
        }
    }
    else
    {
        Say ("$LogHeader Can't find aspnetcore.dll for IIS. Skipping updating ANCM files for IIS server")
    }
}

function Update-File([string]$SourceFilePath, [string]$DestinationFilePath) { 

    $Source = $SourceFilePath
    $Destination = $DestinationFilePath

    $BackupFilePath = $Destination + ".ancm_backup"
    if ($Rollback)
    {
        $Source = $BackupFilePath
    }
        
    $functionName = "Update-File -Rollback:$" + $Rollback.ToString()
    $LogHeader = "[$ScriptFileName::$functionName]"

    if ($ForceToBackup)
    {
        if (Test-Path $BackupFilePath)
        {
            $backupFileRemoved = $false
            if ( ((get-item $DestinationFilePath).CreationTime -gt (get-item $BackupFilePath).CreationTime) -and ((get-item $DestinationFilePath).CreationTime -gt (get-item $SourceFilePath).CreationTime) )
            {
                $backupFileRemoved = $true
                Say ('    Delete the existing "$BackupFilePath" because "$DestinationFilePath" is newer than both "$BackupFilePath" and "$SourceFilePath"')
                Remove-Item $BackupFilePath -Force -Confirm:$false
            }
            else
            {
                Say-Verbose ('     Skipping to delete the existing backupfile because "$DestinationFilePath" is not newer than $BackupFilePath"')
            }
        }
        if ($backupFileRemoved -and (Test-Path $BackupFilePath))
        {
            throw ("$LogHeader Can't delete $BackupFilePath")
        }
    }

    # Do the initial back up before updating file
    if (-Not (Test-Path $BackupFilePath))
    {
        Say ("    Create a backup $BackupFilePath")
        Copy-Item $Destination $BackupFilePath  -Force

        $fileMatched = $null -eq (Compare-Object -ReferenceObject $(Get-Content $Destination) -DifferenceObject $(Get-Content $BackupFilePath))
        if (-not $fileMatched)
        {
            throw ("$LogHeader File not matched!!! $Destination $BackupFilePath")
        }
    }
    if (-Not (Test-Path $BackupFilePath))
    {
        throw ("$LogHeader Can't backup $Source to $BackupFilePath")
    }  

    # Copy file from Source to Destination if those files are different each other
    if (-Not (Test-Path $Destination))
    {
        throw ("$LogHeader Can't find $Destination")
    }
    $fileMatched = $null -eq (Compare-Object -ReferenceObject $(Get-Content $Source) -DifferenceObject $(Get-Content $Destination))
    if (-not $fileMatched)
    {
        Say ("    Copying $Source to $Desting...")
        Copy-Item $Source $Destination -Force

        # check file is correctly copied
        $fileMatched = $null -eq (Compare-Object -ReferenceObject $(Get-Content $Source) -DifferenceObject $(Get-Content $Destination))
        if (-not $fileMatched)
        {
            throw ("$LogHeader File not matched!!! $Source $Destination")
        }
        else
        {
            Say-Verbose ("$LogHeader File matched!!! $Source to $Destination")
        }
    }
    else
    {
        Say ("    Skipping $Destination that is already identical to $Source ")
    }
}

function Say($str) {
    Write-Host $str
}

function Say-Verbose($str) {
    Write-Verbose $str
}

#######################################################
# Start execution point
#######################################################

$EXIT_FAIL = 1
$EXIT_SUCCESS = 0

$ScriptFileName = "installancm.ps1"
$LogHeader = "[$ScriptFileName]"

if ($Extract -and (-Not $Rollback))
{
    if (-not (Test-Path $PackagePath))
    {
        Say ("$LogHeader Error!!! Failed to find the directory $PackagePath")
        exit $EXIT_FAIL
    }

    $ancmNugetFilePath = Get-ANCMNugetFilePath
    if (-not (Test-Path $ancmNugetFilePath))
    {
        Say ("$LogHeader Error!!! Failed to find AspNetCoreModule nupkg file under $PackagePath nor its child directories")
        exit $EXIT_FAIL
    }
}

if (-Not $Rollback)
{
    if (-not (Test-Path $ExtractFilesTo))
    {
        Say ("$LogHeader Error!!! Failed to find the directory $ExtractFilesTo")
        exit $EXIT_FAIL
    }
}

$TempExtractFilesTo = $ExtractFilesTo + "\.ancm"
$ExtractFilesRootPath = ""
if ($Extract)
{
    $ExtractFilesRootPath = $TempExtractFilesTo + "\ancm\Debug"
}
else
{
    $ExtractFilesRootPath = $ExtractFilesTo
}

# Try with solution output path
$aspnetCorex64From = $ExtractFilesRootPath + "\x64\aspnetcore.dll"
$aspnetCoreWin32From = $ExtractFilesRootPath + "\Win32\aspnetcore.dll"
$aspnetCoreSchemax64From = $ExtractFilesRootPath + "\x64\aspnetcore_schema.xml"
$aspnetCoreSchemaWin32From = $ExtractFilesRootPath + "\Win32\aspnetcore_schema.xml"

$aspnetCorex64To = "$env:ProgramFiles\IIS Express\aspnetcore.dll"
$aspnetCoreWin32To = "${env:ProgramFiles(x86)}\IIS Express\aspnetcore.dll"
$aspnetCoreSchemax64To = "$env:ProgramFiles\IIS Express\config\schema\aspnetcore_schema.xml"
$aspnetCoreSchemaWin32To = "${env:ProgramFiles(x86)}\IIS Express\config\schema\aspnetcore_schema.xml"

$aspnetCorex64IISTo = "$env:windir\system32\inetsrv\aspnetcore.dll"
$aspnetCoreWin32IISTo = "$env:windir\syswow64\inetsrv\aspnetcore.dll"
$aspnetCoreSchemax64IISTo = "$env:windir\system32\inetsrv\config\schema\aspnetcore_schema.xml"

# if this is not solution output path, use nuget package directory structure
if (-not (Test-Path $aspnetCorex64From))
{
    $aspnetCorex64From = $ExtractFilesRootPath + "\runtimes\win7-x64\native\aspnetcore.dll"
    $aspnetCoreWin32From = $ExtractFilesRootPath + "\runtimes\win7-x86\native\aspnetcore.dll"
    $aspnetCoreSchemax64From = $ExtractFilesRootPath + "\aspnetcore_schema.xml"
    $aspnetCoreSchemaWin32From = $ExtractFilesRootPath + "\aspnetcore_schema.xml"

    $aspnetCorex64To = "$env:ProgramFiles\IIS Express\aspnetcore.dll"
    $aspnetCoreWin32To = "${env:ProgramFiles(x86)}\IIS Express\aspnetcore.dll"
    $aspnetCoreSchemax64To = "$env:ProgramFiles\IIS Express\config\schema\aspnetcore_schema.xml"
    $aspnetCoreSchemaWin32To = "${env:ProgramFiles(x86)}\IIS Express\config\schema\aspnetcore_schema.xml"

    $aspnetCorex64IISTo = "$env:windir\system32\inetsrv\aspnetcore.dll"
    $aspnetCoreWin32IISTo = "$env:windir\syswow64\inetsrv\aspnetcore.dll"
    $aspnetCoreSchemax64IISTo = "$env:windir\system32\inetsrv\config\schema\aspnetcore_schema.xml"
}

$is64BitMachine = $env:PROCESSOR_ARCHITECTURE.ToLower() -eq "amd64"
$isIISExpressInstalled = Test-Path $aspnetCorex64To
$isIISInstalled = Test-Path $aspnetCorex64IISTo

# Check expected files are available on IIS/IISExpress directory
if (-not (Check-TargetFiles))
{
    Say ("$LogHeader Error!!! Failed to update ANCM files because AspnetCore.dll is not installed on IIS/IISExpress directory.")
    exit $EXIT_FAIL
}

if ($Extract)
{
    # Extrack nuget package when $DoExtract is true
    if (-not (Extract-ANCMFromNugetPackage))
    {
        Say ("$LogHeader Error!!! Failed to extract ANCM file")
        exit $EXIT_FAIL
    }
}

# clean up IIS and IISExpress worker processes and IIS services
Say ("$LogHeader Stopping w3wp.exe process")
Stop-Process -Name w3wp -ErrorAction Ignore -Force -Confirm:$false

Say ("$LogHeader Stopping iisexpress.exe process")
Stop-Process -Name iisexpress -ErrorAction Ignore -Force -Confirm:$false

$w3svcGotStopped = $false
$w3svcWindowsServce = Get-Service W3SVC -ErrorAction Ignore
if ($w3svcWindowsServce -and $w3svcWindowsServce.Status -eq "Running")
{
    Say ("$LogHeader Stopping w3svc service")
    $w3svcGotStopped = $true
    Stop-Service W3SVC -Force -ErrorAction Ignore
    Say ("$LogHeader Stopping w3logsvc service")
    Stop-Service W3LOGSVC -Force -ErrorAction Ignore
}

if ($Rollback)
{
    Say ("$LogHeader Rolling back ANCM files...")
}
else
{
    Say  ("Updating ANCM files...")
}
Update-ANCM

# Recover w3svc service 
if ($w3svcGotStopped)
{
    Say ("$LogHeader Starting w3svc service")
    Start-Service W3SVC -ErrorAction Ignore
    $w3svcServiceStopped = $false

    $w3svcWindowsServce = Get-Service W3SVC -ErrorAction Ignore
    if ($w3svcWindowsServce.Status -ne "Running")
    {
        Say  ("$LogHeader Error!!! Failed to start w3svc service.")
        exit $EXIT_FAIL
    }
}

Say ("$LogHeader Finished!!!")
exit $EXIT_SUCCESS
