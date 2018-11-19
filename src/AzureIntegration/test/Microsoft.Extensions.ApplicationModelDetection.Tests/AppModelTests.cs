// // Copyright (c) .NET Foundation. All rights reserved.
// // Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.Extensions.ApplicationModelDetection.Tests
{
    public class AppModelTests
    {
        private const string  EmptyWebConfig = @"<?xml version=""1.0"" encoding=""utf-8""?><configuration></configuration>";

        [Theory]
        [InlineData("dotnet")]
        [InlineData("dotnet.exe")]
        [InlineData("%HOME%/dotnet")]
        [InlineData("%HOME%/dotnet.exe")]
        [InlineData("DoTNeT.ExE")]
        public void DetectsCoreFrameworkFromWebConfig(string processPath)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config",GenerateWebConfig(processPath, ".\\app.dll")))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCore, result.Framework);
            }
        }

        [Theory]
        [InlineData("app")]
        [InlineData("app.exe")]
        [InlineData("%HOME%/app")]
        [InlineData("%HOME%/app.exe")]
        public void DetectsFullFrameworkFromWebConfig(string processPath)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig(processPath, ".\\app.dll")))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetFramework, result.Framework);
            }
        }

        [Theory]
        [InlineData("2.0.0")]
        [InlineData("2.0.0-preview1")]
        [InlineData("1.1.3")]
        public void DetectsRuntimeVersionFromRuntimeConfig(string runtimeVersion)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig("dotnet", ".\\app.dll"))
                .WithFile("app.runtimeconfig.json", @"{
  ""runtimeOptions"": {
    ""tfm"": ""netcoreapp2.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": """+ runtimeVersion + @"""
    },
    ""configProperties"": {
      ""System.GC.Server"": true
    }
  }
}"))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCore, result.Framework);
                Assert.Equal(runtimeVersion, result.FrameworkVersion);
            }
        }


        [Theory]
        [InlineData("2.0.0")]
        [InlineData("2.0.0-preview1")]
        [InlineData("1.1.3")]
        public void DetectsRuntimeVersionFromRuntimeConfigWitoutEntryPoint(string runtimeVersion)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig("dotnet", "%HOME%\\app.dll"))
                .WithFile("app.runtimeconfig.json", @"{
  ""runtimeOptions"": {
    ""tfm"": ""netcoreapp2.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": """+ runtimeVersion + @"""
    },
    ""configProperties"": {
      ""System.GC.Server"": true
    }
  }
}"))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCore, result.Framework);
                Assert.Equal(runtimeVersion, result.FrameworkVersion);
            }
        }

        [Theory]
        [InlineData("2.0.0")]
        [InlineData("2.0.0-preview1")]
        [InlineData("1.1.3")]
        public void DetectsAspNetCoreVersionFromDepsFile(string runtimeVersion)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig("dotnet", "app.dll"))
                .WithFile("app.deps.json", @"{
  ""targets"": {
    "".NETCoreApp,Version=v2.7"": {
      ""Microsoft.AspNetCore.Hosting/" + runtimeVersion + @""": { }
    }
  }
}"))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCore, result.Framework);
                Assert.Equal(runtimeVersion, result.AspNetCoreVersion);
            }
        }

        [Theory]
        [InlineData("2.0.0")]
        [InlineData("2.0.0-preview1")]
        [InlineData("1.1.3")]
        public void DetectsAspNetCoreVersionFromDepsFileWithoutEntryPoint(string runtimeVersion)
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig("dotnet", "%HOME%\\app.dll"))
                .WithFile("app.deps.json", @"{
  ""targets"": {
    "".NETCoreApp,Version=v2.7"": {
      ""Microsoft.AspNetCore.Hosting/" + runtimeVersion + @""": { }
    }
  }
}"))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCore, result.Framework);
                Assert.Equal(runtimeVersion, result.AspNetCoreVersion);
            }
        }

        [Fact]
        public void DetectsFullFrameworkWhenWebConfigExists()
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", EmptyWebConfig))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetFramework, result.Framework);
            }
        }

        [Fact]
        public void DetectsStandalone_WhenBothDepsAndRuntimeConfigExist()
        {
            using (var temp = new TemporaryDirectory()
                .WithFile("web.config", GenerateWebConfig("app.exe", ""))
                .WithFile("app.runtimeconfig.json", "{}")
                .WithFile("app.deps.json", "{}"))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(RuntimeFramework.DotNetCoreStandalone, result.Framework);
            }
        }

        [Fact]
        public void DetectsAspNetCoreVersionFromHostingDll()
        {
            using (var temp = new TemporaryDirectory()
                .WithFile(typeof(WebHostBuilder).Assembly.Location))
            {
                var result = new AppModelDetector().Detect(temp.Directory);
                Assert.Equal(typeof(WebHostBuilder).Assembly.GetName().Version.ToString(), result.AspNetCoreVersion);
            }
        }

        private static string GenerateWebConfig(string processPath, string arguments)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <system.webServer>
    <handlers>
      <add name=""aspNetCore"" path=""*"" verb=""*"" modules=""AspNetCoreModule"" resourceType=""Unspecified"" />
    </handlers>
    <aspNetCore processPath=""{processPath}"" arguments=""{arguments}"" stdoutLogEnabled=""false"" stdoutLogFile="".\logs\stdout"" />
  </system.webServer>
</configuration>
";
        }

        private class TemporaryDirectory: IDisposable
        {
            public TemporaryDirectory()
            {
                Directory = new DirectoryInfo(Path.GetTempPath())
                    .CreateSubdirectory(Guid.NewGuid().ToString("N"));
            }

            public DirectoryInfo Directory { get; }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(true);
                }
                catch (IOException)
                {
                }
            }

            public TemporaryDirectory WithFile(string name, string value)
            {
                File.WriteAllText(Path.Combine(Directory.FullName, name), value);
                return this;
            }


            public TemporaryDirectory WithFile(string name)
            {
                File.Copy(name, Path.Combine(Directory.FullName, Path.GetFileName(name)));
                return this;
            }
        }
    }
}