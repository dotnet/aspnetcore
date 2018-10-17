param([string]$UserName, [string]$Server = "asp-rpi-01", [switch]$UseExistingKey, [switch]$Force)

$ErrorActionPreference = "Stop"

if(!(Get-Command ssh -ErrorAction SilentlyContinue)) {
    throw "Missing required command: ssh";
}

if(!(Get-Command ssh-keygen -ErrorAction SilentlyContinue)) {
    throw "Missing required command: ssh-keygen";
}

if(!$UserName) {
    if([System.Environment]::UserDomainName -eq "REDMOND") {
        $UserName = [System.Environment]::UserName;
        Write-Host "Auto-detected username as '$UserName'"
    }
    else {
        throw "Cannot detect username, please pass the -UserName switch"
    }
}

$MachineName = [Environment]::MachineName

$KeyFile = Join-Path (Join-Path $env:USERPROFILE ".ssh") "asp-rpi"
$PubKey = "$KeyFile.pub"

if(Test-Path $PubKey) {
    if($UseExistingKey) {
        Write-Host "Using existing key: $PubKey"
    }
    elseif($Force) {
        Remove-Item $KeyFile
        Remove-Item $PubKey
    }
    else {
        throw "A key called asp-rpi already exists on this machine. Specify -Force to delete it"
    }
}

if(!(Test-Path $PubKey)) {
    Write-Host -ForegroundColor Green "Generating key. Please enter a Passphrase for the key when prompted. DO NOT paste the asp-rpi-password here."
    ssh-keygen -t rsa -b 4096 -C "$UserName / $MachineName" -f $KeyFile
}

Write-Host -ForegroundColor Green "Uploading key. Please DO paste the asp-rpi-password value when prompted for the password"
$PubKeyValue = [System.IO.File]::ReadAllText($PubKey).Trim()
Write-Output $PubKeyValue | ssh "pi@$Server" "cat - >> /home/pi/.ssh/authorized_keys"

Write-Host -ForegroundColor Green "Uploaded SSH Key to $Server. You can now use $KeyFile to authenticate with that server."
Write-Host "Use 'ssh -i $KeyFile pi@$Server' to connect"
Write-Host "Or, run 'ssh-add $KeyFile' to add this key to your ssh-agent for automatic authentication"