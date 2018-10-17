Param(
    [Parameter(Mandatory=$true)]
    [string] $machineName,

    [Parameter(Mandatory=$true)]
    [string] $vhdPath,

    [string] $resourceGroupName = "aspnet-ci",
    [string] $storageAccountName = "aspnetci0wriojfnwnw0vhds",
    [string] $vmSize = "Standard_F4s"
    [bool] $isWindows = $true
)

$ErrorActionPreference = "Stop"

$subscriptionName = "ADX Domain Joined"
$vnetName = "Cache-CI-VNET-EX"
$subnetName = "TenantSubnet"

Get-AzureRmSubscription -SubscriptionName $subscriptionName | Select-AzureRmSubscription

$resGroup = Get-AzureRmResourceGroup -Name $resourceGroupName

$storage = Get-AzureRmStorageAccount -Name $storageAccountName -ResourceGroupName $resGroup.ResourceGroupName
$vnet = Get-AzureRmVirtualNetwork -Name $vnetName -ResourceGroupName "cache-ci-jenkins"
$subnet = $vnet.Subnets | Where-Object {$_.Name -eq $subnetName} | Select-Object -First 1

$osDiskPath = $storage.PrimaryEndpoints.Blob.ToString() + $vhdPath

$vm = New-AzureRmVMConfig -VMName $machineName -VMSize $vmSize
$vm = Set-AzureRmVMOSDisk -VM $vm -Name $machineName -VhdUri $osDiskPath -CreateOption Attach -Linux
$vm = Set-AzureRmVMBootDiagnostics -VM $vm -Disable

if ($isWindows)
{
    $vm = Set-AzureRmVMOSDisk -VM $vm -Name $machineName -VhdUri $osDiskPath -CreateOption Attach -Windows
}
else 
{
    $vm = Set-AzureRmVMOSDisk -VM $vm -Name $machineName -VhdUri $osDiskPath -CreateOption Attach -Linux
}

Write-Host "Creating network interface $machineName..."
$nic = New-AzureRmNetworkInterface -Name $machineName -Subnet $subnet -ResourceGroupName $resGroup.ResourceGroupName -Location $resGroup.Location
$vm = Add-AzureRmVMNetworkInterface -VM $vm -NetworkInterface $nic

Write-Host "Creating virtual machine $machineName..."
New-AzureRmVM -VM $vm -ResourceGroupName $resGroup.ResourceGroupName -Location $resGroup.Location 