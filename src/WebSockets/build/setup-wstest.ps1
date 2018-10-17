function has($cmd) { !!(Get-Command $cmd -ErrorAction SilentlyContinue) }

# Download VCForPython27 if necessary
$VendorDir = Join-Path (Get-Location) "vendor"

if(!(Test-Path $VendorDir)) {
    mkdir $VendorDir
}

$VirtualEnvDir = Join-Path $VendorDir "virtualenv";
$ScriptsDir = Join-Path $VirtualEnvDir "Scripts"
$WsTest = Join-Path $ScriptsDir "wstest.exe"

$VCPythonMsi = Join-Path $VendorDir "VCForPython27.msi"
if(!(Test-Path $VCPythonMsi)) {
    Write-Host "Downloading VCForPython27.msi"
    Invoke-WebRequest -Uri https://download.microsoft.com/download/7/9/6/796EF2E4-801B-4FC4-AB28-B59FBF6D907B/VCForPython27.msi -OutFile "$VCPythonMsi"
}
else {
    Write-Host "Using VCForPython27.msi from Cache"
}

# Install VCForPython27
Write-Host "Installing VCForPython27"

# Launch this way to ensure we wait for msiexec to complete. It's a Windows app so it won't block the console by default.
Start-Process msiexec "/i","$VCPythonMsi","/qn","/quiet","/norestart" -Wait

Write-Host "Installed VCForPython27"

# Install Python
if(!(has python)) {
    choco install python2
}

if(!(has python)) {
    throw "Failed to install python2"
}

# Install virtualenv
pip install virtualenv

# Make a virtualenv in .virtualenv
virtualenv $VirtualEnvDir

& "$ScriptsDir\python" --version
& "$ScriptsDir\pip" --version

# Install autobahn into the virtualenv
& "$ScriptsDir\pip" install autobahntestsuite

Write-Host "Using wstest from: '$WsTest'"