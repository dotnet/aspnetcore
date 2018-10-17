. "$PSScriptRoot\_Common.ps1"

Ensure-Msi "Python 2.7" "{9DA28CE5-0AA5-429E-86D8-686ED898C666}" "python-2.7.12.amd64.msi"
Ensure-Msi "Visual C++ Compiler for Python 2.7" "{692514A8-5484-45FC-B0AE-BE2DF7A75891}" "VCForPython27.msi"
Ensure-Path "C:\Python27\Scripts"
Ensure-Path "C:\Python27"

if(!(Get-Command pip -ErrorAction SilentlyContinue)) {
    throw "Python installation failed. Unable to find 'pip'"
}

Write-Host "`nInstalling Autobahn Test Suite (wstest)..."
python -m pip install --upgrade pip # This uses python.exe to avoid locking the pip executable
pip install autobahntestsuite

if(!(Get-Command wstest -ErrorAction SilentlyContinue)) {
    throw "Autobahn installation failed. Unable to find 'wstest'"
}
