// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class AzureDiagnosticsConfigurationProviderTests
{
    [Fact]
    public void NoConfigFile()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "AzureWebAppLoggerThisFolderShouldNotExist");

        var contextMock = new Mock<IWebAppContext>();
        contextMock.SetupGet(c => c.HomeFolder)
            .Returns(tempFolder);

        var config = SiteConfigurationProvider.GetAzureLoggingConfiguration(contextMock.Object);

        Assert.NotNull(config);
    }

    [Fact]
    public void ReadsSettingsFileAndEnvironment()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "WebAppLoggerConfigurationDisabledInSettingsFile");

        try
        {
            var settingsFolder = Path.Combine(tempFolder, "site", "diagnostics");
            var settingsFile = Path.Combine(settingsFolder, "settings.json");

            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
            }
            Environment.SetEnvironmentVariable("RANDOM_ENVIRONMENT_VARIABLE", "USEFUL_VALUE");
            File.WriteAllText(settingsFile, @"{ ""key"":""test value"" }");

            var contextMock = new Mock<IWebAppContext>();
            contextMock.SetupGet(c => c.HomeFolder)
                .Returns(tempFolder);

            var config = SiteConfigurationProvider.GetAzureLoggingConfiguration(contextMock.Object);

            Assert.Equal("test value", config["key"]);
            Assert.Equal("USEFUL_VALUE", config["RANDOM_ENVIRONMENT_VARIABLE"]);
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                try
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
                catch
                {
                    // Don't break the test if temp folder deletion fails.
                }
            }
        }
    }
}
