##########################################################
# NOTE: 
# For running test automation, following prerequisite required:
#
# 1. On Win7, powershell should be upgraded to 4.0
#    https://social.technet.microsoft.com/wiki/contents/articles/21016.how-to-install-windows-powershell-4-0.aspx
# 2. url-rewrite should be installed 
# 3. makecert.exe tools should be available
##########################################################

# Replace aspnetcore.dll with the latest version
copy C:\gitroot\AspNetCoreModule\artifacts\build\AspNetCore\bin\Release\x64\aspnetcore.dll "C:\Program Files\IIS Express"
copy C:\gitroot\AspNetCoreModule\artifacts\build\AspNetCore\bin\Release\x64\aspnetcore.pdb "C:\Program Files\IIS Express"


# Enable appverif for IISExpress.exe
appverif /verify iisexpress.exe

# Set the AspNetCoreModuleTest environment variable with the following command 
cd C:\gitroot\AspNetCoreModule\test\AspNetCoreModule.Test
dotnet restore
dotnet build
$aspNetCoreModuleTest="C:\gitroot\AspNetCoreModule\test\AspNetCoreModule.Test\bin\Debug\net46"

if (Test-Path (Join-Path $aspNetCoreModuleTest aspnetcoremodule.test.dll))
{
    # Clean up applicationhost.config of IISExpress
    del $env:userprofile\documents\iisexpress\config\applicationhost.config -Confirm:$false -Force
    Start-Process "C:\Program Files\IIS Express\iisexpress.exe"
    Sleep 3
    Stop-Process -Name iisexpress

    # Create sites
    (1..50) | foreach { md ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ ) 2> out-null } 
    (1..50) | foreach { copy C:\gitroot\AspNetCoreModule\test\StressTestWebRoot\web.config ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ ) } 
    (1..50) | foreach { 
        $path = ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ ) 
        $appPath = "/foo"+$_
         & "C:\Program Files\IIS Express\appcmd.exe" add app /site.name:"WebSite1" /path:$appPath /physicalPath:$path
    }

    <#(1..50) | foreach { 
        $configpath = ("WebSite1/foo" + $_)
        $value = "C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ + ".exe"
        & "C:\Program Files\IIS Express\appcmd.exe" set config $configpath -section:system.webServer/aspNetCore /processPath:$value
    }
    (1..50) | foreach { copy C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo.exe ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ +".exe") } 
    (1..50) | foreach { 
        $configpath = ("WebSite1/foo" + $_)
        $value = "%AspNetCoreModuleTest%\AspnetCoreApp_HelloWeb\foo" + $_ + ".exe"
        & "C:\Program Files\IIS Express\appcmd.exe" set config $configpath -section:system.webServer/aspNetCore /processPath:$value /apphostconfig:%AspNetCoreModuleTest%\config\applicationhost.config

        $value = "%AspNetCoreModuleTest%\AspnetCoreApp_HelloWeb\AutobahnTestServer.dll"
        & "C:\Program Files\IIS Express\appcmd.exe" set config $configpath -section:system.webServer/aspNetCore /arguments:$value /apphostconfig:%AspNetCoreModuleTest%\config\applicationhost.config
    } 
    #>
    
    # Start IISExpress with running the below command
    &"C:\Program Files\Debugging Tools for Windows (x64)\windbg.exe" /g /G "C:\Program Files\IIS Express\iisexpress.exe"


    # 6. Start stress testing
    (1..10000) | foreach {
        if ($_ % 2 -eq 0)
        {
            ("Recycling backend only")
            stop-process -name dotnet 
            (1..50) | foreach { del ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_  + "\app_offline.htm") -confirm:$false -Force 2> out-null } 
            stop-process -name dotnet 
        }
        else
        {
            ("Recycling backedn + enabling appoffline ....")          
            stop-process -name dotnet
            (1..50) | foreach { copy C:\gitroot\AspNetCoreModule\test\StressTestWebRoot\app_offline.htm ("C:\inetpub\wwwroot\AspnetCoreHandler_HelloWeb\foo" + $_ ) } 
        }
        Sleep 1

        (1..10) | foreach {
            (1..50) | foreach { 
                invoke-webrequest ("http://localhost:8080/foo"+$_) > $null
            }
        }
    }


    # Stress test idea
    # 1. Use Web Stress Tester
    # 2. Run stop-process -name dotnet
    # 3. Hit Q command to IISExpress console window
    # 4. Use app_offline.htm
    # 5. Save dummy web.config 
}

// bp aspnetcore!FORWARDING_HANDLER::FORWARDING_HANDLER
// bp aspnetcore!FORWARDING_HANDLER::~FORWARDING_HANDLER