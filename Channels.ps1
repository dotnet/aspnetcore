param(
        [Parameter(Mandatory=$true,Position=0)]
        [ValidateSet('release','vnext','volatile')]
        $desiredFeed
    )

function Use-Feed{
    [cmdletbinding()]
    param(
        [Parameter(Mandatory=$true,Position=0)]
        [ValidateSet('release','vnext','volatile')]
        $desiredFeed
    )
    process{
    $feedUrl = "https://www.myget.org/F/aspnet$desiredFeed/api/v2"

    Write-Host "Setting environment variable DNX_FEED to $feedUrl"
    [Environment]::SetEnvironmentVariable("DNX_FEED", $feedUrl, [System.EnvironmentVariableTarget]::User)
    
    $nugetConfigPath = [IO.Path]::Combine($env:AppData, "NuGet", "NuGet.config")
    [xml]$configXml = Get-Content $nugetConfigPath

    $configuration = $configXml.configuration
    $keyPrefix = "ASP.NET 5"
    $aspNet5FeedNames = "vnext", "volatile", "release"

    $packageSources = $configuration.packageSources
    
    Write-Host "Updating $nugetConfigPath"

    # Create the disabledPackageSources element if it doesn't exist
    if ($configXml.configuration.disabledPackageSources -eq $null)
    {
        Write-Host "  Creating disabledPackageSources element"
        $disabledPackageSources = $configXml.CreateElement("disabledPackageSources")
        $configXml.configuration.AppendChild($disabledPackageSources) | Out-Null
    }

    ForEach ($feed in $aspNet5FeedNames)
    {
        $fullFeedName = "$keyPrefix ($feed)"
        $currentFeed = $configXml.configuration.packageSources.add | Where-Object key -eq $fullFeedName
    
        if ($currentFeed -eq $null)
        {
            # Add package source
            $newFeed = $configXml.CreateElement("add")
            $newFeed.Attributes.Append($configXml.CreateAttribute("key")) | Out-Null
            $newFeed.key = "$keyPrefix ($feed)"
            $newFeed.Attributes.Append($configXml.CreateAttribute("value")) | Out-Null
            $newFeed.value = "https://www.myget.org/F/aspnet$feed/api/v2"

            Write-Host "  Adding '$fullFeedName' to packageSources"
            $configXml.configuration.packageSources.AppendChild($newFeed) | out-null
        }

        $currentFeed = $configXml.configuration.disabledPackageSources.add | Where-Object key -eq $fullFeedName

        if (($currentFeed -eq $null) -and (-Not ($feed -eq $desiredFeed)))
        {
            # Add disabled packaeg source
            $newFeed = $configXml.CreateElement("add")
            $newFeed.Attributes.Append($configXml.CreateAttribute("key")) | Out-Null
            $newFeed.key = "$keyPrefix ($feed)"
            $newFeed.Attributes.Append($configXml.CreateAttribute("value")) | Out-Null
            $newFeed.value = "true"

            Write-Host "  Adding '$fullFeedName' to disabledPackageSources"
            $configXml.configuration.disabledPackageSources.AppendChild($newFeed) | Out-Null
        }
        elseif ((-Not ($currentFeed -eq $null)) -and ($feed -eq $desiredFeed))
        {
            # Remove desired feed from disabledPackageSources list
            Write-Host "  Removing '$fullFeedName' from disabledPackageSources"
            $configXml.configuration.disabledPackageSources.RemoveChild($currentFeed) | Out-Null
        }
    }

    Write-Host "  Saving $nugetConfigPath"
    $configXml.Save($nugetConfigPath) | out-null

    dnvm.cmd upgrade
    dnvm.cmd install default -r coreclr
    dnvm.cmd use default -r clr -p
    }
}

Use-Feed $desiredFeed
