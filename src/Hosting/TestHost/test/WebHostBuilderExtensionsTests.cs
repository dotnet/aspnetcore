// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.TestHost;

public class WebHostBuilderExtensionsTests
{
    [Fact]
    public void UseSolutionRelativeContentRoot_FallsBackToSlnx()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "slnx-" + Guid.NewGuid().ToString("n"));
        var applicationBasePath = Path.Combine(rootPath, "src", "MyApp", "bin");

        try
        {
            Directory.CreateDirectory(applicationBasePath);
            File.WriteAllText(Path.Combine(rootPath, "Test.slnx"), string.Empty);
            Directory.CreateDirectory(Path.Combine(rootPath, "src", "MyApp"));

            var builder = new WebHostBuilder();

            builder.UseSolutionRelativeContentRoot("src/MyApp", applicationBasePath, "*.sln");

            Assert.Equal(Path.GetFullPath(Path.Combine(rootPath, "src", "MyApp")), builder.GetSetting(WebHostDefaults.ContentRootKey));
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public void UseSolutionRelativeContentRoot_PrefersSlnOverSlnx()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "sln-" + Guid.NewGuid().ToString("n"));
        var applicationBasePath = Path.Combine(rootPath, "child", "content", "bin");

        try
        {
            Directory.CreateDirectory(applicationBasePath);
            File.WriteAllText(Path.Combine(rootPath, "Parent.sln"), string.Empty);
            File.WriteAllText(Path.Combine(rootPath, "child", "Child.slnx"), string.Empty);
            Directory.CreateDirectory(Path.Combine(rootPath, "parentContent"));

            var builder = new WebHostBuilder();

            builder.UseSolutionRelativeContentRoot("parentContent", applicationBasePath, "*.sln");

            Assert.Equal(Path.GetFullPath(Path.Combine(rootPath, "parentContent")), builder.GetSetting(WebHostDefaults.ContentRootKey));
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }
}
