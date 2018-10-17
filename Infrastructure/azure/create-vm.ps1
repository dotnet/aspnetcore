Param(
    [Parameter(Mandatory=$true)]
    [string] $machineName,

    [Parameter(Mandatory=$true)]
    [ValidateSet("win2008", "win2012", "ubuntu1404")]
    [string] $os,

    [string] $resourceGroupName = "aspnet-ci",
    [string] $storageAccountName = "aspnetci0wriojfnwnw0vhds",
    [string] $vmSize = "Standard_F4s"
)

$ErrorActionPreference = "Stop"

$subscriptionName = "ADX Domain Joined"
$vnetName = "Cache-CI-VNET-EX"
$subnetName = "TenantSubnet"

$sourceImagePublisher = ""
$sourceImageOffer = ""
$sourceImageSku = ""

$isWindows = $true
switch($os) {
    "win2008"
    {
        $sourceImagePublisher = "MicrosoftWindowsServer"
        $sourceImageOffer = "WindowsServer"
        $sourceImageSku = "2008-R2-SP1"
    }
    "win2012" 
    {
        $sourceImagePublisher = "MicrosoftWindowsServer"
        $sourceImageOffer = "WindowsServer"
        $sourceImageSku = "2012-R2-Datacenter"
    }
    "ubuntu1404" 
    { 
        $isWindows = $false
        $sourceImagePublisher = "Canonical"
        $sourceImageOffer = "UbuntuServer"
        $sourceImageSku = "14.04.4-LTS"
    }
    default 
    { 
        throw "Unknown OS $os" 
    }
}

$localCredentials = Get-Credential -Message "Type the user name and password for the VM local administrator" -UserName "aspnetagent"

Get-AzureRmSubscription -SubscriptionName $subscriptionName | Select-AzureRmSubscription

$resGroup = Get-AzureRmResourceGroup -Name $resourceGroupName

$storage = Get-AzureRmStorageAccount -Name $storageAccountName -ResourceGroupName $resGroup.ResourceGroupName
$vnet = Get-AzureRmVirtualNetwork -Name $vnetName -ResourceGroupName "cache-ci-jenkins"
$subnet = $vnet.Subnets | Where-Object {$_.Name -eq $subnetName} | Select-Object -First 1

$osDiskPath = $storage.PrimaryEndpoints.Blob.ToString() + "vhds/" + $machineName + ".vhd"

$vm = New-AzureRmVMConfig -VMName $machineName -VMSize $vmSize
$vm = Set-AzureRmVMSourceImage -VM $vm -PublisherName $sourceImagePublisher -Offer $sourceImageOffer -Skus $sourceImageSku -Version "latest"
$vm = Set-AzureRmVMOSDisk -VM $vm -Name $machineName -VhdUri $osDiskPath -CreateOption FromImage

$vm = Set-AzureRmVMBootDiagnostics -VM $vm -Disable

if ($isWindows)
{
    $vm = Set-AzureRmVMOperatingSystem -VM $vm -Windows -ComputerName $machineName -Credential $localCredentials -ProvisionVMAgent -EnableAutoUpdate -TimeZone "Pacific Standard Time"
}
else 
{
    $vm = Set-AzureRmVMOperatingSystem -VM $vm -Linux -ComputerName $machineName -Credential $localCredentials
}

Write-Host "Creating network interface $machineName..."
$nic = New-AzureRmNetworkInterface -Name $machineName -Subnet $subnet -ResourceGroupName $resGroup.ResourceGroupName -Location $resGroup.Location
$vm = Add-AzureRmVMNetworkInterface -VM $vm -NetworkInterface $nic

Write-Host "Creating virtual machine $machineName..."
New-AzureRmVM -VM $vm -ResourceGroupName $resGroup.ResourceGroupName -Location $resGroup.Location 