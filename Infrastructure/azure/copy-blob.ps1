Param(
    [Parameter(Mandatory=$true)][string] $srcAccountName,
    [Parameter(Mandatory=$true)][string] $srcAccountKey,
    [Parameter(Mandatory=$true)][string] $srcContainerName,
    [Parameter(Mandatory=$true)][string] $srcBlobName,

    [Parameter(Mandatory=$true)][string] $destAccountName,
    [Parameter(Mandatory=$true)][string] $destAccountKey,
    [string] $destContainerName = $null,
    [string] $destBlobName = $null
)

$ErrorActionPreference = "Stop"

if ($destContainerName -eq $null) {
    $destContainerName = $srcContainerName
}
if ($destBlobName -eq $null) {
    $destBlobName = $srcBlobName
}

$fullSrcPath = "$srcAccountName/$srcContainerName/$srcBlobName"
$fullDestPath = "$destAccountName/$destContainerName/$destBlobName"

$caption = "Confirm"    
$message = "Are you sure you want to copy '$fullSrcPath' to '$fullDestPath' ?"
$yes = New-Object System.Management.Automation.Host.ChoiceDescription "&Yes", ""
$no = New-Object System.Management.Automation.Host.ChoiceDescription "&No", ""
$options = [System.Management.Automation.Host.ChoiceDescription[]]($yes, $no)
[int]$defaultChoice = 1
$choiceRTN = $host.ui.PromptForChoice($caption, $message, $options, $defaultChoice)

if ( $choiceRTN -eq $defaultChoice )
{
    exit 0
}

Select-AzureSubscription "ADX Domain Joined"

$srcContext = New-AzureStorageContext -StorageAccountName $srcAccountName -StorageAccountKey $srcAccountKey
$destContext = New-AzureStorageContext -StorageAccountName $destAccountName -StorageAccountKey $destAccountKey

$copyOp = Start-CopyAzureStorageBlob -SrcContext $srcContext -SrcContainer $srcContainerName -SrcBlob $srcBlobName -DestContext $destContext -DestContainer $destContainerName -DestBlob $destBlobName

$state = $copyOp | Get-AzureStorageBlobCopyState

while($state.Status -eq "Pending")
{
    $state = $copyOp | Get-AzureStorageBlobCopyState

    $state

    Start-Sleep -Seconds 10
}

Write-Host "Done"