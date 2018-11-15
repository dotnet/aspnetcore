param(
    [string] $server = $env:TEST_SERVER,
    [string] $userName = $env:TEST_SERVER_USER,
    [string] $password = $env:TEST_SERVER_PASS,
    [string] $serverFolder = "dev"
)
$ErrorActionPreference = "Stop"

$projectFile = "MusicStore.Test\MusicStore.Test.csproj"

Write-Host "Test server:  $server"
Write-Host "Test folder:  $serverFolder"

$projectName = (get-item $projectFile).Directory.Name
Write-Host "Test project: $projectName"

Invoke-Expression "..\tools\BundleAndDeploy.ps1 -projectFile $projectFile -server $server -serverFolder $serverFolder -userName $userName -password `"$password`""

$pass = ConvertTo-SecureString $password -AsPlainText -Force
$cred = New-Object System.Management.Automation.PSCredential ($userName, $pass);

Set-Item WSMan:\localhost\Client\TrustedHosts "$server" -Force

#This block of code will be executed remotely
$remoteScript = {
    $ErrorActionPreference = "Continue"
    cd C:\$using:serverFolder\$using:projectName
    dir
    $env:DNX_TRACE=1

    $output = & .\approot\test.cmd 2>&1
    $output
    $lastexitcode
}

Write-Host ">>>> Remote code execution started <<<<"
$remoteJob = Invoke-Command -ComputerName $server -Credential $cred -ScriptBlock $remoteScript -AsJob
Wait-Job $remoteJob
Write-Host "<<<< Remote execution code completed >>>>"

Write-Host ">>>> Remote execution output <<<<"
$remoteResult = Receive-Job $remoteJob
$remoteResult
Write-Host "<<<< End of remote execution output >>>>"

$finalExitCode = $remoteResult[$remoteResult.Length-1]
exit $finalExitCode
