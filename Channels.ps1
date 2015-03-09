function Use-Volatile
{
    Use-Feed "volatile"
}

function Use-Release
{
    Use-Feed "release"
}

function Use-Dev
{
    Use-Feed "vnext"
}

function Use-Feed($feed)
{
    [Reflection.Assembly]::LoadWithPartialName("System.Xml.Linq") | Out-Null

    if ($env:DOTNET_FEED)
    {
        rm env:DOTNET_FEED
    }
    if ($env:KRE_FEED)
    {
        rm env:KRE_FEED
    }

    $env:DNX_FEED = $feed

    $nugetConfigPath = [IO.Path]::Combine($env:AppData, "NuGet", "NuGet.config")
    $configFile = [System.Xml.Linq.XDocument]::Load($nugetConfigPath)
    
    $configuration = $configFile.Root

    $keyPrefix = "ASP.NET 5"
    $keyFormat = "$keyPrefix ({0})"
    $aspNet5FeedNames = "vnext", "volatile", "release"

    # Ensure all the ASP.NET 5 feeds are configured
    $packageSources = $configuration.Descendants("packageSources").FirstOrDefault();
    if ($packageSources -eq $null)
    {
        $packageSources = New-Object System.Xml.Linq.XElement([System.Xml.Linq.XName]"packageSources");
        $configuration.Add($packageSources);
    }

    
    kvm upgrade
    kvm install default -r coreclr
    kvm use default -r clr -p
}