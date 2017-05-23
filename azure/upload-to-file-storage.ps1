[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $accountName,
    [Parameter(Mandatory=$true)][string] $accountKey,
    [Parameter(Mandatory=$true)][string] $shareName,
    [Parameter(Mandatory=$true)][string] $localFile,
    [Parameter(Mandatory=$true)][string] $targetFile
)

$context = New-AzureStorageContext $accountName $accountKey
Set-AzureStorageFileContent -ShareName $shareName -Source $localFile -Path $targetFile -Context $context