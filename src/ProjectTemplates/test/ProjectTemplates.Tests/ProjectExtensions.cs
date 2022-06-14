// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Templates.Test.Helpers;

namespace Templates.Test;

public static class ProjectExtensions
{
    public static async Task<Project> VerifyLaunchSettings(this Project project, string[] expectedLaunchProfileNames)
    {
        var launchSettingsFiles = Directory.EnumerateFiles(project.TemplateOutputDir, "launchSettings.json", SearchOption.AllDirectories);

        foreach (var filePath in launchSettingsFiles)
        {
            using var launchSettingsFile = File.OpenRead(filePath);
            using var launchSettings = await JsonDocument.ParseAsync(launchSettingsFile);

            var profiles = launchSettings.RootElement.GetProperty("profiles");
            foreach (var profileName in expectedLaunchProfileNames)
            {
                Assert.True(profiles.TryGetProperty(profileName, out var _), $"Expected launch profile '{profileName}' was not found in file {filePath}");
            }

            if (launchSettings.RootElement.TryGetProperty("iisSettings", out var iisSettings)
                && iisSettings.TryGetProperty("iisExpress", out var iisExpressSettings))
            {
                var iisSslPort = iisExpressSettings.GetProperty("sslPort").GetInt32();
                if (expectedLaunchProfileNames.Contains("https"))
                {
                    Assert.True(iisSslPort > 44300, $"IIS Express port was expected to be greater than 44300 but was {iisSslPort} in file {filePath}");
                }
                else
                {
                    Assert.Equal(0, iisSslPort);
                }
            }
        }

        return project;
    }
}
