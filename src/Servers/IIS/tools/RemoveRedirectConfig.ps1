(Get-Content C:\Windows\System32\inetsrv\config\redirection.config) -replace 'enabled="true"', 'enabled="false"' | Out-File C:\Windows\System32\inetsrv\config\redirection.config
