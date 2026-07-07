// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Tools.NativeAot.Tests;

public class NativeAotToolScenarioTests
{
    private static readonly string TestTargetFramework = GetTestTargetFramework();
    private readonly ITestOutputHelper _output;

    public NativeAotToolScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [ConditionalFact]
    [SkipOnHelix("Native AOT publishing is not supported on these queues.", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task UserSecretsRunsMainlineProjectSecretWorkflow()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        temporaryDirectory.Create();

        var environment = NativeAotToolRunner.CreateIsolatedUserProfileEnvironment(temporaryDirectory);
        var project = CreateUserSecretsProject(Path.Combine(temporaryDirectory.Root, "UserSecretsProject"), userSecretsId: null);

        var init = await RunAsync("dotnet-user-secrets", environment, "init", "--project", project);
        Assert.Equal(0, init.ExitCode);

        var projectXml = XDocument.Load(project);
        var userSecretsId = Assert.Single(projectXml.Descendants("UserSecretsId"));
        Assert.False(string.IsNullOrEmpty(userSecretsId.Value));

        var set = await RunAsync("dotnet-user-secrets", environment, "set", "ApiKey", "aot-secret", "--project", project);
        Assert.Equal(0, set.ExitCode);
        Assert.Contains("Successfully saved ApiKey to the secret store.", set.AllOutput);

        var list = await RunAsync("dotnet-user-secrets", environment, "list", "--project", project);
        Assert.Equal(0, list.ExitCode);
        Assert.Contains("ApiKey = aot-secret", list.AllOutput);

        var remove = await RunAsync("dotnet-user-secrets", environment, "remove", "ApiKey", "--project", project);
        Assert.Equal(0, remove.ExitCode);

        list = await RunAsync("dotnet-user-secrets", environment, "list", "--project", project);
        Assert.Equal(0, list.ExitCode);
        Assert.Contains("No secrets configured for this application.", list.AllOutput);

        set = await RunAsync("dotnet-user-secrets", environment, "set", "ApiKey", "aot-secret", "--project", project);
        Assert.Equal(0, set.ExitCode);

        var clear = await RunAsync("dotnet-user-secrets", environment, "clear", "--project", project);
        Assert.Equal(0, clear.ExitCode);

        list = await RunAsync("dotnet-user-secrets", environment, "list", "--project", project);
        Assert.Equal(0, list.ExitCode);
        Assert.Contains("No secrets configured for this application.", list.AllOutput);
    }

    [ConditionalFact]
    [SkipOnHelix("Native AOT publishing is not supported on these queues.", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task UserJwtsRunsMainlineTokenWorkflow()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        temporaryDirectory.Create();

        var environment = NativeAotToolRunner.CreateIsolatedUserProfileEnvironment(temporaryDirectory);
        var project = CreateUserJwtsProject(temporaryDirectory.Root);
        var projectDirectory = Path.GetDirectoryName(project);
        var appSettingsFile = Path.Combine(projectDirectory, "appsettings.Development.json");

        var create = await RunAsync("dotnet-user-jwts", environment, "create", "--project", project, "--scheme", "NativeAotScheme", "--output", "json");
        Assert.Equal(0, create.ExitCode);

        var jwt = JsonNode.Parse(create.StandardOutput).AsObject();
        Assert.Equal("NativeAotScheme", jwt["Scheme"].GetValue<string>());
        Assert.False(string.IsNullOrEmpty(jwt["Token"].GetValue<string>()));
        Assert.Contains("NativeAotScheme", File.ReadAllText(appSettingsFile));

        var list = await RunAsync("dotnet-user-jwts", environment, "list", "--project", project, "--output", "json");
        Assert.Equal(0, list.ExitCode);

        var jwts = JsonNode.Parse(list.StandardOutput).AsObject();
        var listedJwt = Assert.Single(jwts).Value.AsObject();
        Assert.Equal("NativeAotScheme", listedJwt["Scheme"].GetValue<string>());

        var clear = await RunAsync("dotnet-user-jwts", environment, "clear", "--project", project, "--force");
        Assert.Equal(0, clear.ExitCode);

        list = await RunAsync("dotnet-user-jwts", environment, "list", "--project", project, "--output", "json");
        Assert.Equal(0, list.ExitCode);

        var emptyList = JsonNode.Parse(list.StandardOutput).AsArray();
        Assert.Empty(emptyList);
    }

    [ConditionalFact]
    [SkipOnHelix("Native AOT publishing is not supported on these queues.", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task DevCertsRunsMachineReadableCertificateCheck()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        temporaryDirectory.Create();

        var result = await RunAsync(
            "dotnet-dev-certs",
            NativeAotToolRunner.CreateIsolatedUserProfileEnvironment(temporaryDirectory),
            "https",
            "--check-trust-machine-readable");

        Assert.Equal(0, result.ExitCode);

        var certificates = JsonNode.Parse(result.StandardOutput).AsArray();
        foreach (var certificate in certificates)
        {
            var certificateReport = certificate.AsObject();
            Assert.NotNull(certificateReport["Subject"]);
            Assert.NotNull(certificateReport["IsHttpsDevelopmentCertificate"]);
            Assert.NotNull(certificateReport["TrustLevel"]);
        }
    }

    private Task<NativeAotToolResult> RunAsync(string toolName, IReadOnlyDictionary<string, string> environment, params string[] arguments)
        => NativeAotToolRunner.RunAsync(toolName, arguments, _output, environmentVariables: environment);

    private static string CreateUserSecretsProject(string projectDirectory, string userSecretsId)
    {
        Directory.CreateDirectory(projectDirectory);
        var userSecretsProperty = string.IsNullOrEmpty(userSecretsId) ? string.Empty : $"<UserSecretsId>{userSecretsId}</UserSecretsId>";
        var projectPath = Path.Combine(projectDirectory, "TestProject.csproj");

        File.WriteAllText(
            projectPath,
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>{{TestTargetFramework}}</TargetFramework>
                {{userSecretsProperty}}
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
              </PropertyGroup>
            </Project>
            """);

        return projectPath;
    }

    private static string GetTestTargetFramework()
    {
        var targetFrameworkName = typeof(NativeAotToolScenarioTests).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (string.IsNullOrEmpty(targetFrameworkName))
        {
            throw new InvalidOperationException($"Could not determine the target framework for {nameof(NativeAotToolScenarioTests)}.");
        }

        var frameworkName = new FrameworkName(targetFrameworkName);

        return $"net{frameworkName.Version.Major}.{frameworkName.Version.Minor}";
    }

    private static string CreateUserJwtsProject(string root)
    {
        var projectDirectory = Directory.CreateDirectory(Path.Combine(root, "UserJwtsProject")).FullName;
        Directory.CreateDirectory(Path.Combine(projectDirectory, "Properties"));

        var projectPath = CreateUserSecretsProject(projectDirectory, Guid.NewGuid().ToString("N"));

        File.WriteAllText(
            Path.Combine(projectDirectory, "Properties", "launchSettings.json"),
            """
            {
              "profiles": {
                "HttpWebApp": {
                  "commandName": "Project",
                  "applicationUrl": "https://localhost:5001;http://localhost:5000"
                }
              }
            }
            """);

        File.WriteAllText(Path.Combine(projectDirectory, "appsettings.Development.json"), "{}");

        return projectPath;
    }
}
