$agents = @("aspnetci-a00", "aspnetci-a01", "aspnetci-a02", "aspnetci-a03", "ASPNETCI-A05", "ASPNETCI-A07", "aspnetci-a08", "aspnetci-a09", "aspnetci-a12", "ASPNETCI-A14", "aspnetci-a17", "ASPNETCI-A18")
$sourcefile = "C:\tools\AspNetCoreModule\v0.8-rc2\aspnetcoremodule_x64_en.msi"
$creds = Get-Credential "redmond\asplab"

foreach ($agent in $agents)
{
    Write-Host $agent
    $remote = New-PSDrive -Name Remote -PSProvider FileSystem -Root "\\$agent\C$" -Credential $creds
    
    Try
    {
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
            & cmd /c "msiexec.exe /i C:\temp\AspNetCoreModule\aspnetcoremodule_x64_en.msi" /quiet
            $lastexitcode
        }
        $remoteJob = Invoke-Command -Credential $creds -ComputerName $agent -ScriptBlock $remoteScript -AsJob
        Wait-Job $remoteJob
        $remoteResult = Receive-Job $remoteJob
        Write-Host $remoteResult
        $finalExitCode = $remoteResult[$remoteResult.Length-1]
        if ($finalExitCode -eq 0)
        {
            Write-Host "Installed Successfully"
        }
        else
        {
            Write-Host "Install Failed"
            exit $finalExitCode
        }

        #Copy the schema files over
        Copy-Item -Path "${remote}:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "${remote}:\Program Files\IIS Express\config\schema\aspnetcore_schema.xml" -Force
        Copy-Item -Path "${remote}:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Destination "${remote}:\Program Files (x86)\IIS Express\config\schema\aspnetcore_schema.xml" -Force
        Write-Host "Copied schema successfully"
    }
    Finally
    {
        Remove-PSDrive -Name Remote
    }
}