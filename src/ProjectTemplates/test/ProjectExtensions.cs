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
        using var launchSettingsFile = File.OpenRead(Path.Combine(project.TemplateOutputDir, "Properties", "launchSettings.json"));
        using var launchSettings = await JsonDocument.ParseAsync(launchSettingsFile);
        var iisExpressSettings = launchSettings.RootElement.GetProperty("iisSettings").GetProperty("iisExpress");
        var iisSslPort = iisExpressSettings.GetProperty("sslPort").GetInt32();

        var profiles = launchSettings.RootElement.GetProperty("profiles");
        foreach (var profileName in expectedLaunchProfileNames)
        {
            Assert.True(profiles.TryGetProperty(profileName, out var _));
        }

        if (expectedLaunchProfileNames.Contains("https"))
        {
            Assert.True(iisSslPort > 44300);
        }
        else
        {
            Assert.Equal(0, iisSslPort);
        }

        return project;
    }
}
