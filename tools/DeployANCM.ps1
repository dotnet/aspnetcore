$agents = @("ASPNETCI-A05", "ASPNETCI-A07")
$sourcefile = "\\funfile\Scratch\panwang\aspnetcoremodule\x64-03-08-N\aspnetcoremodule.msi"
$creds = Get-Credential "redmond\asplab"

foreach ($agent in $agents)
{
    $agent
    $remote = New-PSDrive -Name Remote -PSProvider FileSystem -Root "\\$agent\C$" -Credential $creds
    
    $destinationFolder = "${remote}:\temp\AspNetCoreModule"
    #This section will copy the $sourcefile to the $destinationfolder. If the Folder does not exist it will create it.
    if (!(Test-Path -path $destinationFolder))
    {
        New-Item $destinationFolder -Type Directory
        Write-Host "Directory created" $destinationFolder
    }
    Copy-Item -Path $sourcefile -Destination $destinationFolder
    Write-Host "Copied msi successfully"

    $remoteScript = {
        & cmd /c "msiexec.exe /i C:\temp\AspNetCoreModule\aspnetcoremodule.msi" /quiet
        $lastexitcode
    }
    $remoteJob = Invoke-Command -Credential $creds -ComputerName $agent -ScriptBlock $remoteScript -AsJob
    Wait-Job $remoteJob
    $remoteResult = Receive-Job $remoteJob
    $remoteResult
    $finalExitCode = $remoteResult[$remoteResult.Length-1]
    if ($finalExitCode -eq 0)
    {
        Write-Host "Installed Successfully"
    }
    else
    {
        Write-Host "Installed Failed"
        exit $finalExitCode
    }

    #Copy the schema files over
    Copy-Item -Path "${remote}:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "${remote}:\Program Files\IIS Express\config\schema\aspnetcore_schema.xml" -Force
    Copy-Item -Path "${remote}:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "${remote}:\Program Files (x86)\IIS Express\config\schema\aspnetcore_schema.xml" -Force
    Write-Host "Copied schema successfully"

    Remove-PSDrive -Name Remote
}