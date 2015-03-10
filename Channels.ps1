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

function Use-Feed($desiredFeed)
{
    $nugetConfigPath = [IO.Path]::Combine($env:AppData, "NuGet", "NuGet.config")
    [xml]$configXml = Get-Content $nugetConfigPath

    $configuration = $configXml.configuration
    $keyPrefix = "ASP.NET 5"
    $aspNet5FeedNames = "vnext", "volatile", "release"

    $packageSources = $configuration.packageSources
    
    # Create the disabledPackageSources element if it doesn't exist
    if ($configXml.configuration.disabledPackageSources -eq $null)
    {
        $disabledPackageSources = $configXml.CreateElement("disabledPackageSources")
        $configXml.configuration.AppendChild($disabledPackageSources)
    }

    ForEach ($feed in $aspNet5FeedNames)
    {
        $fullFeedName = "$keyPrefix ($feed)"
        $currentFeed = $configXml.configuration.packageSources.add | Where-Object key -eq $fullFeedName
    
        if ($currentFeed -eq $null)
        {
            # Add package source
            $newFeed = $configXml.CreateElement("add")
            $newFeed.Attributes.Append($configXml.CreateAttribute("key"))
            $newFeed.key = "$keyPrefix ($feed)"
            $newFeed.Attributes.Append($configXml.CreateAttribute("value"))
            $newFeed.value = "https://www.myget.org/F/aspnet$feed/api/v2"
            $configXml.configuration.packageSources.AppendChild($newFeed)
        }

        $currentFeed = $configXml.configuration.disabledPackageSources.add | Where-Object key -eq $fullFeedName

        if ($currentFeed -eq $null -and (-Not ($feed -eq $desiredFeed)))
        {
            # Add disabled packaeg source
            $newFeed = $configXml.CreateElement("add")
            $newFeed.Attributes.Append($configXml.CreateAttribute("key"))
            $newFeed.key = "$keyPrefix ($feed)"
            $newFeed.Attributes.Append($configXml.CreateAttribute("value"))
            $newFeed.value = "true"
            $configXml.configuration.disabledPackageSources.AppendChild($newFeed)
        }
        elseif ((-Not ($currentFeed -eq $null)) -and $feed -eq $desiredFeed)
        {
            # Remove desired feed from disabledPackageSources list
            $configXml.configuration.disabledPackageSources.RemoveChild($currentFeed)
        }
    }

    $configXml.Save($nugetConfigPath)

    dnvm upgrade
    dnvm install default -r coreclr
    dnvm use default -r clr -p
}