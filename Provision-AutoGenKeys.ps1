param (
    [Parameter(Mandatory = $True)]
    [string] $appPoolName
  )

# Provisions the HKLM registry so that the specified user account can persist auto-generated machine keys.
function Provision-AutoGenKeys {
  [CmdletBinding()]
  param (
    [ValidateSet("2.0", "4.0")]
    [Parameter(Mandatory = $True)]
    [string] $frameworkVersion,
    [ValidateSet("32", "64")]
    [Parameter(Mandatory = $True)]
    [string] $architecture,
    [Parameter(Mandatory = $True)]
    [string] $sid
  )
  process {
    # We require administrative permissions to continue.
    if (-Not (new-object System.Security.Principal.WindowsPrincipal([System.Security.Principal.WindowsIdentity]::GetCurrent())).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Error "This cmdlet requires Administrator permissions."
        return
    }
    # Open HKLM with an appropriate view into the registry
    if ($architecture -eq "32") {
        $regView = [Microsoft.Win32.RegistryView]::Registry32;
    } else {
        $regView = [Microsoft.Win32.RegistryView]::Registry64;
    }
    $baseRegKey = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, $regView)
    # Open ASP.NET base key
    if ($frameworkVersion -eq "2.0") {
        $expandedVersion = "2.0.50727.0"
    } else {
        $expandedVersion = "4.0.30319.0"
    }
    $softwareMicrosoftKey = $baseRegKey.OpenSubKey("SOFTWARE\Microsoft\", $True);

    $aspNetKey = $softwareMicrosoftKey.OpenSubKey("ASP.NET", $True);
    if ($aspNetKey -eq $null)
    {
        $aspNetKey = $softwareMicrosoftKey.CreateSubKey("ASP.NET")
    }

    $aspNetBaseKey = $aspNetKey.OpenSubKey("$expandedVersion", $True);
    if ($aspNetBaseKey -eq $null)
    {
        $aspNetBaseKey = $aspNetKey.CreateSubKey("$expandedVersion")
    }

    # Create AutoGenKeys subkey if it doesn't already exist
    $autoGenBaseKey = $aspNetBaseKey.OpenSubKey("AutoGenKeys", $True)
    if ($autoGenBaseKey -eq $null) {
        $autoGenBaseKey = $aspNetBaseKey.CreateSubKey("AutoGenKeys")
    }
    # SYSTEM, ADMINISTRATORS, and the target SID get full access
    $regSec = New-Object System.Security.AccessControl.RegistrySecurity
    $regSec.SetSecurityDescriptorSddlForm("D:P(A;OICI;GA;;;SY)(A;OICI;GA;;;BA)(A;OICI;GA;;;$sid)")
    $userAutoGenKey = $autoGenBaseKey.OpenSubKey($sid, $True)
    if ($userAutoGenKey -eq $null) {
        # Subkey didn't exist; create and ACL appropriately
        $userAutoGenKey = $autoGenBaseKey.CreateSubKey($sid, [Microsoft.Win32.RegistryKeyPermissionCheck]::Default, $regSec)
    } else {
        # Subkey existed; make sure ACLs are correct
        $userAutoGenKey.SetAccessControl($regSec)
    }
  }
}

$ErrorActionPreference = "Stop"
if (Get-Command Get-IISAppPool -errorAction SilentlyContinue)
{
    $processModel = (Get-IISAppPool $appPoolName).processModel
}
else
{
    Import-Module WebAdministration
    $processModel = Get-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel"
}

$identityType = $processModel.identityType
Write-Output "Pool process model: '$identityType'"

Switch ($identityType)
{
    "LocalService" {
        $userName = "LocalService";
    }
    "LocalSystem" {
        $userName = "System";
    }
    "NetworkService" {
        $userName = "NetworkService";
    }
    "ApplicationPoolIdentity" {
        $userName = "IIS APPPOOL\$appPoolName";
    }
    "SpecificUser" {
        $userName = $processModel.userName;
    }
}
Write-Output "Pool user name: '$userName'"

Try
{
    $poolSid = (New-Object System.Security.Principal.NTAccount($userName)).Translate([System.Security.Principal.SecurityIdentifier]).Value
}
Catch [System.Security.Principal.IdentityNotMappedException]
{
    Write-Error "Application pool '$appPoolName' account cannot be resolved."
}

Write-Output "Pool SID: '$poolSid'"

Provision-AutoGenKeys "4.0" "32" $poolSid
Provision-AutoGenKeys "4.0" "64" $poolSid
