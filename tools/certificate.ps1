# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

<##############################################################################
 Example
 
 ###############################################################################
 # Create a new root certificate on "Cert:\LocalMachine\My" and export it to "Cert:\LocalMachine\Root"
 # FYI, you can do the same thing with one of the following commands:
 #   %sdxroot\tools\amd64\MakeCert.exe -r -pe -n "CN=ANCMTest_Root" -b 12/22/2013 -e 12/23/2020 -ss root -sr localmachine -len 2048 -a sha256
 #   $thumbPrint = (New-SelfSignedCertificate -DnsName "ANCMTest_Root", "ANCMTest_Roo3" -CertStoreLocation "cert:\LocalMachine\My").Thumbprint
 ###############################################################################
 $rootSubject = "ANCMTest_Root"
 $thumbPrint = .\certificate.ps1 -Command Create-SelfSignedCertificate -Subject $rootSubject
 .\certificate.ps1 -Command Export-CertificateTo -TargetThumbPrint $thumbPrint -TargetSSLStore "Cert:\LocalMachine\My" -ExportToSSLStore "Cert:\LocalMachine\Root"
 .\certificate.ps1 -Command Get-CertificateThumbPrint -Subject $rootSubject -TargetSSLStore "Cert:\LocalMachine\Root"
  
 ###############################################################################
 # Create a new certificate setting issuer with the root certicate's subject name on "Cert:\LocalMachine\My" and export it to "Cert:\LocalMachine\Root"
 # FYI, you can do the same thing with one of the following commands:
 #   %sdxroot\tools\amd64\MakeCert.exe -pe -n "CN=ANCMTestWebServer" -b 12/22/2013 -e 12/23/2020  -eku 1.3.6.1.5.5.7.3.1 -is root -ir localmachine -in $rootSubject -len 2048 -ss my -sr localmachine -a sha256
 #   %sdxroot\tools\amd64\MakeCert.exe -pe -n "CN=ANCMTest_Client" -eku 1.3.6.1.5.5.7.3.2 -is root -ir localmachine -in ANCMTest_Root -ss my -sr currentuser -len 2048 -a sha256
 ###############################################################################
 $childSubject = "ANCMTest_Client"
 $thumbPrint2 = .\certificate.ps1 -Command Create-SelfSignedCertificate -Subject $childSubject -IssuerName $rootSubject
 ("Result: $thumbPrint2")
 .\certificate.ps1 -Command Export-CertificateTo -TargetThumbPrint $thumbPrint2 -TargetSSLStore "Cert:\LocalMachine\My" -ExportToSSLStore "Cert:\CurrentUser\My"

 .\certificate.ps1 -Command Export-CertificateTo -TargetThumbPrint $thumbPrint2 -TargetSSLStore "Cert:\LocalMachine\My" -ExportToSSLStore C:\gitroot\AspNetCoreModule\tools\test.pfx -PfxPassword test


 # Clean up
 .\certificate.ps1 -Command Delete-Certificate -TargetThumbPrint $thumbPrint2 -TargetSSLStore "Cert:\LocalMachine\My"
 .\certificate.ps1 -Command Delete-Certificate -TargetThumbPrint $thumbPrint2 -TargetSSLStore "Cert:\CurrentUser\Root"
 .\certificate.ps1 -Command Delete-Certificate -TargetThumbPrint $thumbPrint -TargetSSLStore "Cert:\LocalMachine\My"
 .\certificate.ps1 -Command Delete-Certificate -TargetThumbPrint $thumbPrint -TargetSSLStore "Cert:\LocalMachine\Root"

###############################################################################>


Param(
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Create-SelfSignedCertificate",
                 "Delete-Certificate",
                 "Export-CertificateTo",
                 "Get-CertificateThumbPrint",
                 "Get-CertificatePublicKey")]
    [string]
    $Command,

    [parameter()]
    [string]
    $Subject,

    [parameter()]
    [string]
    $IssuerName,

    [Parameter()]
    [string]
    $FriendlyName = "", 

    [Parameter()]
    [string[]]
    $AlternativeNames = "",

    [Parameter()]
    [string]
    $TargetSSLStore = "",
    
    [Parameter()]
    [string]
    $ExportToSSLStore = "",
    
    [Parameter()]
    [string]
    $PfxPassword = "",

    [Parameter()]
    [string]
    $TargetThumbPrint = ""
)

function Create-SelfSignedCertificate($_subject, $_friendlyName, $_alternativeNames, $_issuerName) {

    if (-not $_subject)
    {
        return ("Error!!! _subject is required")
    }

    #
    # $_issuerName should be set with the value subject and its certificate path will be root path
    if (-not $_issuerName)
    {
        $_issuerName = $_subject
    }

    #
    # Create $subjectDn and $issuerDn
    $subjectDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $subjectDn.Encode( "CN=" + $_subject, $subjectDn.X500NameFlags.X500NameFlags.XCN_CERT_NAME_STR_NONE)
    $issuerDn = new-object -com "X509Enrollment.CX500DistinguishedName"
    $issuerDn.Encode("CN=" + $_issuerName, $subjectDn.X500NameFlags.X500NameFlags.XCN_CERT_NAME_STR_NONE)
    
    #
    # Create a new Private Key
    $key = new-object -com "X509Enrollment.CX509PrivateKey"
    $key.ProviderName =  "Microsoft Enhanced RSA and AES Cryptographic Provider"    
    # XCN_AT_SIGNATURE, The key can be used for signing
    $key.KeySpec = 2
    $key.Length = 2048
    # MachineContext 0: Current User, 1: Local Machine
    $key.MachineContext = 1
    $key.Create() 

    # 
    # Create a cert object with the newly created private key
    $cert = new-object -com "X509Enrollment.CX509CertificateRequestCertificate"
    $cert.InitializeFromPrivateKey(2, $key, "")
    $cert.Subject = $subjectDn
    $cert.Issuer = $issuerDn
    $cert.NotBefore = (get-date).AddMinutes(-10)
    $cert.NotAfter = $cert.NotBefore.AddYears(2)
            
    #Use Sha256
    $hashAlgorithm = New-Object -ComObject X509Enrollment.CObjectId
    $hashAlgorithm.InitializeFromAlgorithmName(1,0,0,"SHA256")
    $cert.HashAlgorithm = $hashAlgorithm    
	 
    #
    # Key usage should be set for non-root certificate
    if ($_issuerName -ne $_subject)
    {
        #
        # Extended key usage 
        $clientAuthOid = New-Object -ComObject "X509Enrollment.CObjectId"
        $clientAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.2")
        $serverAuthOid = new-object -com "X509Enrollment.CObjectId"
        $serverAuthOid.InitializeFromValue("1.3.6.1.5.5.7.3.1")
        $ekuOids = new-object -com "X509Enrollment.CObjectIds.1"
        $ekuOids.add($clientAuthOid)
        $ekuOids.add($serverAuthOid)
        $ekuExt = new-object -com "X509Enrollment.CX509ExtensionEnhancedKeyUsage"
        $ekuExt.InitializeEncode($ekuOids)
        $cert.X509Extensions.Add($ekuext)
    	
        #
        #Set Key usage
        $keyUsage = New-Object -com "X509Enrollment.cx509extensionkeyusage"
        # XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE
        $flags = 0x20
        # XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE
        $flags = $flags -bor 0x80
        $keyUsage.InitializeEncode($flags)
        $cert.X509Extensions.Add($keyUsage)
    }
        
    #
    # Subject alternative names
    if ($_alternativeNames -ne $null) {
        $names =  new-object -com "X509Enrollment.CAlternativeNames"
        $altNames = new-object -com "X509Enrollment.CX509ExtensionAlternativeNames"
        foreach ($n in $_alternativeNames) {
            $name = new-object -com "X509Enrollment.CAlternativeName"
            # Dns Alternative Name
            $name.InitializeFromString(3, $n)
            $names.Add($name)
        }
        $altNames.InitializeEncode($names)
        $cert.X509Extensions.Add($altNames)
    }

    $cert.Encode()

    #$locator = $(New-Object "System.Guid").ToString()
    $locator = [guid]::NewGuid().ToString()
    $enrollment = new-object -com "X509Enrollment.CX509Enrollment"    
    $enrollment.CertificateFriendlyName = $locator        
    $enrollment.InitializeFromRequest($cert)
    $certdata = $enrollment.CreateRequest(0)
    $enrollment.InstallResponse(2, $certdata, 0, "")

    # Wait for certificate to be populated
    $end = $(Get-Date).AddSeconds(1)
    do {
        $Certificates = Get-ChildItem Cert:\LocalMachine\My
        foreach ($item in $Certificates)
        {
            if ($item.FriendlyName -eq $locator)
            {
                $CACertificate = $item
            }
        }
    } while ($CACertificate -eq $null -and $(Get-Date) -lt $end)

    $thumbPrint = ""
    if ($CACertificate -and $CACertificate.Thumbprint)
    {
        $thumbPrint = $CACertificate.Thumbprint.Trim()
    }
    return $thumbPrint
}

function Delete-Certificate($_targetThumbPrint, $_targetSSLStore = $TargetSSLStore) {

    if (-not $_targetThumbPrint)
    {
        return ("Error!!! _targetThumbPrint is required")
    }

    if (Test-Path "$_targetSSLStore\$_targetThumbPrint")
    {
        Remove-Item "$_targetSSLStore\$_targetThumbPrint" -Force -Confirm:$false
    }

    if (Test-Path "$_targetSSLStore\$_targetThumbPrint")
    {
        return ("Error!!! Failed to delete a certificate of $_targetThumbPrint")
    }
}

function Export-CertificateTo($_targetThumbPrint, $_exportToSSLStore, $_password)
{
    if (-not $_targetThumbPrint)
    {
        return ("Error!!! _targetThumbPrint is required")
    }

    if (-not (Test-Path "$TargetSSLStore\$_targetThumbPrint"))
    {
        return ("Error!!! Export failed. Can't find target certificate: $TargetSSLStore\$_targetThumbPrint")
    }
        
    $cert = Get-Item "$TargetSSLStore\$_targetThumbPrint"
    $tempExportFile = "$env:temp\_tempCertificate.cer"
    if (Test-Path $tempExportFile)
    {
        Remove-Item $tempExportFile -Force -Confirm:$false
    }
    
    $isThisWin7 = $false
    $exportToSSLStoreName = $null
    $exportToSSLStoreLocation = $null
    $targetSSLStoreName = $null
    $targetSSLStoreLocation = $null
    
    if ((Get-Command Export-Certificate 2> out-null) -eq $null)
    {
        $isThisWin7 = $true
    }
    
    # if _exportToSSLStore points to a .pfx file
    if ($exportToSSLStore.ToLower().EndsWith(".pfx"))
    {
        if (-not $_password)
        {
            return ("Error!!! _password is required")
        }

        if ($isThisWin7)
        {  
            if ($TargetSSLStore.ToLower().Contains("my"))
            {
                $targetSSLStoreName = "My"
            }
            elseif ($_exportToSSLStore.ToLower().Contains("root"))
            {
                $targetSSLStoreName = "Root"
            }
            else
            {
                throw ("Unsupported store name " + $TargetSSLStore)
            }
            if ($TargetSSLStore.ToLower().Contains("localmachine"))
            {
                $targetSSLStoreLocation = "LocalMachine"
            }
            else
            {
                throw ("Unsupported store location name " + $TargetSSLStore)
            }

            &certutil.exe @('-exportpfx', '-p', $_password, $targetSSLStoreName, $_targetThumbPrint, $_exportToSSLStore) | out-null
            
            if ( Test-Path $_exportToSSLStore )
            {
                # Succeeded to export to .pfx file
                return 
            }
            else
            {
                return ("Error!!! Can't export $TargetSSLStore\$_targetThumbPrint to $tempExportFile")
            }
        }
        else
        {
            $securedPassword = ConvertTo-SecureString -String $_password -Force –AsPlainText 
            $exportedPfxFile = Export-PfxCertificate -FilePath $_exportToSSLStore -Cert $TargetSSLStore\$_targetThumbPrint -Password $securedPassword
            if ( ($exportedPfxFile -ne $null) -and (Test-Path $exportedPfxFile.FullName) )
            {
                # Succeeded to export to .pfx file
                return 
            }
            else
            {
                return ("Error!!! Can't export $TargetSSLStore\$_targetThumbPrint to $tempExportFile")
            }
        }
    }

    if ($isThisWin7)
    {
        # Initialize variables for Win7
        if ($_exportToSSLStore.ToLower().Contains("my"))
        {
            $exportToSSLStoreName = [System.Security.Cryptography.X509Certificates.StoreName]::My
        }
        elseif ($_exportToSSLStore.ToLower().Contains("root"))
        {
            $exportToSSLStoreName = [System.Security.Cryptography.X509Certificates.StoreName]::Root
        }
        else
        {
            throw ("Unsupported store name " + $_exportToSSLStore)
        }
        if ($_exportToSSLStore.ToLower().Contains("localmachine"))
        {
            $exportToSSLStoreLocation = [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine
        }
        elseif ($_exportToSSLStore.ToLower().Contains("currentuser"))
        {
            $exportToSSLStoreLocation = [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser
        }
        else
        {
            throw ("Unsupported store location name " + $_exportToSSLStore)
        }

        # Export-Certificate is not available. 
        $isThisWin7 = $true
        $certificate = Get-Item "$TargetSSLStore\$_targetThumbPrint"
        $base64certificate = @"
-----BEGIN CERTIFICATE-----
$([Convert]::ToBase64String($certificate.Export('Cert'), [System.Base64FormattingOptions]::InsertLineBreaks)))
-----END CERTIFICATE-----
"@
        Set-Content -Path $tempExportFile -Value $base64certificate | Out-Null
    }
    else 
    {
        Export-Certificate -Cert $cert -FilePath $tempExportFile | Out-Null
        if (-not (Test-Path $tempExportFile))
        {
            return ("Error!!! Can't export $TargetSSLStore\$_targetThumbPrint to $tempExportFile")
        }
    }

    if ($isThisWin7)
    {
        [Reflection.Assembly]::Load("System.Security, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b03f5f7f11d50a3a") | Out-Null
        $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($tempExportFile)
        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($exportToSSLStoreName,$exportToSSLStoreLocation)
        $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite) | Out-Null
        $store.Add($cert) | Out-Null
    }
    else
    {
        # clean up destination SSL store
        Delete-Certificate $_targetThumbPrint $_exportToSSLStore
        if (Test-Path "$_exportToSSLStore\$_targetThumbPrint")
        {
            return ("Error!!! Can't delete already existing one $_exportToSSLStore\$_targetThumbPrint")
        }
        Import-Certificate -CertStoreLocation $_exportToSSLStore -FilePath $tempExportFile | Out-Null
    }

    Sleep 3
    if (-not (Test-Path "$_exportToSSLStore\$_targetThumbPrint"))
    {
        return ("Error!!! Can't copy $TargetSSLStore\$_targetThumbPrint to $_exportToSSLStore")
    }
}

function Get-CertificateThumbPrint($_subject, $_issuerName, $_targetSSLStore)
{
    if (-not $_subject)
    {
        return ("Error!!! _subject is required")
    }
    if (-not $_targetSSLStore)
    {
        return ("Error!!! _targetSSLStore is required")
    }

    if (-not (Test-Path "$_targetSSLStore"))
    {
        return ("Error!!! Can't find target store")
    }

    $targetCertificate = $null
    
    $Certificates = Get-ChildItem $_targetSSLStore
    foreach ($item in $Certificates)
    {
        $findItem = $false
        # check subject name first
        if ($item.Subject.ToLower() -eq "CN=$_subject".ToLower())
        {            
            $findItem = $true
        }

        # check issuerName as well
        if ($_issuerName -and $item.Issuer.ToLower() -ne "CN=$_issuerName".ToLower())
        {
            $findItem = $false
        }

        if ($findItem)
        {
            $targetCertificate = $item
            break
        }
    }
    $result = ""
    if ($targetCertificate)
    {
        $result = $targetCertificate.Thumbprint
    }
    else
    {
        ("Error!!! Can't find target certificate")
    }
    return $result
}

function Get-CertificatePublicKey($_targetThumbPrint)
{
    if (-not $_targetThumbPrint)
    {
        return ("Error!!! _targetThumbPrint is required")
    }

    if (-not (Test-Path "$TargetSSLStore\$_targetThumbPrint"))
    {
        return ("Error!!! Can't find target certificate")
    }

    $cert = Get-Item "$TargetSSLStore\$_targetThumbPrint"
    $byteArray = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    $publicKey = [System.Convert]::ToBase64String($byteArray).Trim()

    return $publicKey
}

# Error handling and initializing default values
if (-not $TargetSSLStore)
{
    $TargetSSLStore = "Cert:\LocalMachine\My"
}
else
{
    if ($Command -eq "Create-SelfSignedCertificate")
    {
        return ("Error!!! Create-SelfSignedCertificate should use default value for -TargetSSLStore if -Issuer is not provided")
    }
}

if (-not $ExportToSSLStore)
{
    $ExportToSSLStore = "Cert:\LocalMachine\Root"
}

switch ($Command)
{
    "Create-SelfSignedCertificate"
    {
        return Create-SelfSignedCertificate $Subject $FriendlyName $AlternativeNames $IssuerName
    }
    "Delete-Certificate"
    {
        return Delete-Certificate $TargetThumbPrint
    }
    "Export-CertificateTo"
    {
        return Export-CertificateTo $TargetThumbPrint $ExportToSSLStore $PfxPassword
    }
    "Get-CertificateThumbPrint"
    {
        return Get-CertificateThumbPrint $Subject $IssuerName $TargetSSLStore
    }
    "Get-CertificatePublicKey"
    {
        return Get-CertificatePublicKey $TargetThumbPrint
    }
    default
    {
        throw "Unknown command"
    }
}
