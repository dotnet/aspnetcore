$ErrorActionPreference = "Stop"

$domainCredentials = Get-Credential -Message "Enter domain credentials" -UserName "redmond\asplab"
Add-Computer -DomainName "redmond.corp.microsoft.com" -Credential $domainCredentials

net Localgroup administrators /add $domainCredentials.UserName
net Localgroup "Remote Desktop Users" /add $domainCredentials.UserName
