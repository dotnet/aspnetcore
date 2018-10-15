#
# Generates a new test cert in a .pfx file
# Obviously, don't actually use this to produce production certs
#

param(
    [Parameter(Mandatory = $true)]
    $OutFile
)

$password = ConvertTo-SecureString -Force -AsPlainText -String "password"
$cert = New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation Cert:\CurrentUser\My\
Export-PfxCertificate -Cert $cert -Password $password -FilePath $OutFile
Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)"
