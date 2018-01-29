$sourcefile = "C:\code\aspnetcoremodule\src\AspNetCore\aspnetcore_schema.xml"
$creds = Get-Credential "redmond\asplab"
Import-Module -Scope Local -Force $PSScriptRoot/agentlist.psm1

$agents = Get-Agents | ? { ($_.OS -eq 'Windows') } | % { $_.Name }

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
        Write-Host "Copied xml successfully"

        #Copy the schema files over
        Copy-Item -Path "${remote}:\temp\AspNetCoreModule\aspnetcore_schema.xml" -Destination "${remote}:\Program Files\IIS Express\config\schema\aspnetcore_schema.xml" -Force
        Copy-Item -Path "${remote}:\temp\AspNetCoreModule\aspnetcore_schema.xml" -Destination "${remote}:\Program Files (x86)\IIS Express\config\schema\aspnetcore_schema.xml" -Force
        Copy-Item -Path "${remote}:\temp\AspNetCoreModule\aspnetcore_schema.xml" -Destination "${remote}:\Windows\System32\inetsrv\config\schema\aspnetcore_schema.xml" -Force
        Write-Host "Copied schema successfully"
    }
    Finally
    {
        Remove-PSDrive -Name Remote
    }
}