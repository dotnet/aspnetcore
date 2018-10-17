param(
    [string]$buildAgentName = $env:COMPUTERNAME,

    [string]$teamAgentServiceAccountName = "redmond\asplab",

    [Parameter(Mandatory = $true)]
    [string]$teamAgentServiceAccountPassword,

    [string]$setupFilesShare = "\\aspnetci\share\BuildAgentSetupFiles"
)

$firewallPort = 9090

$buildAgentFolder = "c:\buildagent"
Write-Host "Copying TeamCity build agent files..."
Copy "$setupFilesShare\buildAgent" "c:\" -Recurse

$buildAgentExe = "$buildAgentFolder\launcher\bin\TeamCityAgentService-windows-x86-32.exe"
Write-Host "Adding Inbound and Outbound rules for port $firewallPort..."
New-NetFirewallRule -DisplayName "Allow TeamCityBuildAgent In for $firewallPort" -Direction Inbound -Program $buildAgentExe -Action Allow -Protocol TCP -LocalPort $firewallPort
New-NetFirewallRule -DisplayName "Allow TeamCityBuildAgent Out for $firewallPort" -Direction Outbound -Program $buildAgentExe -Action Allow -Protocol TCP -LocalPort $firewallPort

Write-Host "`nUpdating build agent name and server url..."
$agentPropertiesFile = "$buildAgentFolder\conf\buildAgent.properties"
(Get-Content $agentPropertiesFile).replace('#AGENT_NAME#', $buildAgentName).replace('#SERVER_URL#', "http://aspnetci/").replace('#WORK_DIR#', "C\:\\b\\w").replace('#TEMP_DIR#', "C\:\\b\\t").replace('#SYSTEM_DIR#', "C\:\\BuildAgent\\system") | Set-Content $agentPropertiesFile

Write-Host "Enable File and Printer sharing firewall rule..."
netsh advfirewall firewall set rule group="File and Printer Sharing" new enable=Yes

Write-Host "Installing Java SDK 1.8..."
Copy "$setupFilesShare\javasdk18" "c:\" -Recurse
$args = "INSTALLCFG=c:\javasdk18\InstallConfig"
Start-Process -FilePath "c:\javasdk18\jdk-8u91-windows-x64.exe" -ArgumentList $args -Wait
Del "c:\javasdk18" -Force -Recurse

Write-Host "`nInstalling Node..."
Copy "$setupFilesShare\node-v4.4.5-x64.msi" "c:\"
$args = "/i c:\node-v4.4.5-x64.msi /qn"
Start-Process -FilePath msiexec.exe -ArgumentList $args -Wait
Del "c:\node-v4.4.5-x64.msi" -Force

# Reload the environment variables so that 'npm' is in the path
Write-Host "`nReloading Path environment variable..."
$env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User") 

Write-Host "`nInstalling Bower globally..."
npm install -g bower

Write-Host "`nInstalling Grunt globally..."
npm install -g grunt-cli

Write-Host "`nInstalling Gulp globally..."
npm install -g gulp

Write-Host "`nInstalling Typescript globally..."
npm install -g typescript
npm install -g tsd

Write-Host "`nInstalling Git..."
Copy "$setupFilesShare\Git-2.17.1.2-64-bit.exe" "c:\"
$args = "/SILENT /COMPONENTS='icons,ext\reg\shellhere,assoc,assoc_sh'"
Start-Process -FilePath "c:\Git-2.17.1.2-64-bit.exe" -ArgumentList $args -Wait
del "c:\Git-2.17.1.2-64-bit.exe" -Force

#TODO: Visual Studio's WTE install should install ANCM and following should not be required. 

#Write-Host "`nInstalling ASP.NET Core Module..."
#copy "$setupFilesShare\aspnetcoremodule\V0.9.1965\aspnetcoremodule_x64_en.msi" "c:\"
#$args="/i 'C:\aspnetcoremodule_x64_en.msi' /qn"
#Start-Process -FilePath msiexec.exe -ArgumentList $args -Wait
#Del "C:\aspnetcoremodule_x64_en.msi" -Force

#Copy the schema files over
#Copy-Item -Path "C:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "C:\Program Files\IIS Express\config\schema\aspnetcore_schema.xml" -Force
#Copy-Item -Path "C:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "C:\Program Files (x86)\IIS Express\config\schema\aspnetcore_schema.xml" -Force

Write-Host "`nCopying SSH keys to $env:USERPROFILE..."
Copy "$setupFilesShare\.ssh" "$env:USERPROFILE" -Recurse

Write-Host "`Cloning Coherence-signed repo..."
cd "c:\"
git clone http://github.com/aspnet/coherence-signed
cd coherence-signed
git checkout dev

$changePasswordScript = "c:\coherence-signed\tools\ChangePassword.ps1"

Write-Host "`nInstalling TeamCity build agent service..."
$binFolder = "$buildAgentFolder\bin"
cd $binFolder
& .\service.install.bat
& .\service.start.bat

& "$PSScriptRoot\Components\EnsureAutobahn.ps1" -setupFilesShare:$setupFilesShare

Write-Host "`nUpdating TeamCity service account password configuration value and Starting the team city agent service..."
& $changePasswordScript -teamAgentServiceAccountName $teamAgentServiceAccountName -teamAgentServiceAccountPassword $teamAgentServiceAccountPassword 

# This JDK is a later version than the one installed above. I'm not entirely sure what that one is for, but it's installed to a special directory (C:\Java\jre) and isn't the version SignalR wants to use.
Write-Host "`nEnsure JDK 10 is installed, for SignalR"
& "$PSScriptRoot\EnsureJDK.ps1"
