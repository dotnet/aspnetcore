$ErrorActionPreference = 'Stop'

if (-not $PSScriptRoot) {
    # Older versions of Powershell do not define this variable
    $PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
}

# The certificate thumbprint of TestCert.pfx
$thumb = 'CBA8EF428446072286B8201C2877F5EF0EA3B804'

& certutil -f -v -p testpassword -importpfx "$PSScriptRoot\TestCert.pfx"
if ($lastexitcode -ne 0) {
    throw 'Failed to import test certificate into machine root store. This is required for IIS Express tests.'
}

$tempFile = [System.IO.Path]::GetTempFileName();
$content = "";

for ($i=44300; $i -le 44399; $i++) {
    $content += "http delete sslcert ipport=0.0.0.0:$i`n";
    $content += "http add sslcert ipport=0.0.0.0:$i certhash=$thumb appid=`{214124cd-d05b-4309-9af9-9caa44b2b74a`}`n";
}

[IO.File]::WriteAllLines($tempFile, $content)

netsh -f $tempFile
Remove-Item $tempFile;
