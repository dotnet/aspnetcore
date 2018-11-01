$cert = New-SelfSignedCertificate -DnsName "localhost", "localhost" -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(5)
$thumb = $cert.GetCertHashString()

$Store = New-Object  -TypeName System.Security.Cryptography.X509Certificates.X509Store -ArgumentList 'root', 'LocalMachine'
$Store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$Store.Add($cert)
$Store.Close()

$tempFile = [System.IO.Path]::GetTempFileName();
$content = "";

for ($i=44300; $i -le 44399; $i++) {
    $content += "http delete sslcert ipport=0.0.0.0:$i`n";
    $content += "http add sslcert ipport=0.0.0.0:$i certhash=$thumb appid=`{214124cd-d05b-4309-9af9-9caa44b2b74a`}`n";
}

[IO.File]::WriteAllLines($tempFile, $content)

netsh -f $tempFile
Remove-Item $tempFile;