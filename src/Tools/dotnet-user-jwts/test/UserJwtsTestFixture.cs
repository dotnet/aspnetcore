// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools.Tests;

public sealed class UserJwtsTestFixture : IDisposable
{
    private readonly Stack<Action> _disposables = new();
    internal string TestSecretsId { get; private set; }

    private const string ProjectTemplate = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    {0}
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
</Project>";

    private const string LaunchSettingsTemplate = @"
{
  ""iisSettings"": {
    ""windowsAuthentication"": false,
    ""anonymousAuthentication"": true,
    ""iisExpress"": {
      ""applicationUrl"": ""http://localhost:23528"",
      ""sslPort"": 44395
    }
  },
  ""profiles"": {
    ""HttpWebApp"": {
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""applicationUrl"": ""https://localhost:5001;http://localhost:5000"",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    },
    ""HttpsOnly"": {
      ""commandName"": ""Project"",
      ""dotnetRunMessages"": true,
      ""launchBrowser"": true,
      ""applicationUrl"": ""https://localhost:5001"",
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    },
    ""IIS Express"": {
      ""commandName"": ""IISExpress"",
      ""launchBrowser"": true,
      ""environmentVariables"": {
        ""ASPNETCORE_ENVIRONMENT"": ""Development""
      }
    }
  }
}";

    public string CreateProject(bool hasSecret = true)
    {
        var projectPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "userjwtstest", Guid.NewGuid().ToString()));
        Directory.CreateDirectory(Path.Combine(projectPath.FullName, "Properties"));
        TestSecretsId = Guid.NewGuid().ToString("N");
        var prop = hasSecret ? $"<UserSecretsId>{TestSecretsId}</UserSecretsId>" : string.Empty;
        if (hasSecret)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(TestSecretsId)));
        }

        File.WriteAllText(
            Path.Combine(projectPath.FullName, "TestProject.csproj"),
            string.Format(CultureInfo.InvariantCulture, ProjectTemplate, prop));

        File.WriteAllText(Path.Combine(projectPath.FullName, "Properties", "launchSettings.json"),
            LaunchSettingsTemplate);

        File.WriteAllText(
            Path.Combine(projectPath.FullName, "appsettings.Development.json"),
            "{}");

        if (hasSecret)
        {
            _disposables.Push(() =>
            {
                try
                {
                    var secretsDir = Path.GetDirectoryName(PathHelper.GetSecretsPathFromSecretsId(TestSecretsId));
                    TryDelete(TestSecretsId);
                }
                catch { }
            });
        }

        _disposables.Push(() => TryDelete(projectPath.FullName));

        return projectPath.FullName;
    }

    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to delete " + directory);
        }
    }

    public void Dispose()
    {
        while (_disposables.Count > 0)
        {
            _disposables.Pop()?.Invoke();
        }
    }
}
