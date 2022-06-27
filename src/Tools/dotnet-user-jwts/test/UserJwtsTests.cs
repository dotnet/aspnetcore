// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Tools.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
using Xunit;
using Xunit.Abstractions;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools.Tests;

public class UserJwtsTests : IClassFixture<UserJwtsTestFixture>
{
    private readonly TestConsole _console;
    private readonly UserJwtsTestFixture _fixture;
    private readonly ITestOutputHelper _testOut;

    public UserJwtsTests(UserJwtsTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _testOut = output;
        _console = new TestConsole(output);
    }

    [Fact]
    public void List_NoTokensForNewProject()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "list", "--project", project });
        Assert.Contains("No JWTs created yet!", _console.GetOutput());
    }

    [Fact]
    public void List_HandlesNoSecretsInProject()
    {
        var project = Path.Combine(_fixture.CreateProject(false), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "list", "--project", project });
        Assert.Contains("Set UserSecretsId to ", _console.GetOutput());
        Assert.Contains("No JWTs created yet!", _console.GetOutput());
    }

    [Fact]
    public void Create_CreatesSecretOnNoSecretInproject()
    {
        var project = Path.Combine(_fixture.CreateProject(false), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var output = _console.GetOutput();
        Assert.DoesNotContain("could not find SecretManager.targets", output);
        Assert.Contains("Set UserSecretsId to ", output);
        Assert.Contains("New JWT saved", output);
    }

    [Fact]
    public void Create_WritesGeneratedTokenToDisk()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        Assert.Contains("New JWT saved", _console.GetOutput());
        Assert.Contains("dotnet-user-jwts", File.ReadAllText(appsettings));
    }

    [Fact]
    public void Print_ReturnsNothingForMissingToken()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "print", "invalid-id", "--project", project });
        Assert.Contains("No token with ID 'invalid-id' found", _console.GetOutput());
    }

    [Fact]
    public void List_ReturnsIdForGeneratedToken()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--scheme", "MyCustomScheme" });
        Assert.Contains("New JWT saved", _console.GetOutput());

        app.Run(new[] { "list", "--project", project });
        Assert.Contains("MyCustomScheme", _console.GetOutput());
    }

    [Fact]
    public void Remove_RemovesGeneratedToken()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        app.Run(new[] { "create", "--project", project, "--scheme", "Scheme2" });

        app.Run(new[] { "remove", id, "--project", project });
        var appsettingsContent = File.ReadAllText(appsettings);
        Assert.DoesNotContain("Bearer", appsettingsContent);
        Assert.Contains("Scheme2", appsettingsContent);
    }

    [Fact]
    public void Clear_RemovesGeneratedTokens()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        app.Run(new[] { "create", "--project", project, "--scheme", "Scheme2" });

        Assert.Contains("New JWT saved", _console.GetOutput());

        app.Run(new[] { "clear", "--project", project, "--force" });
        var appsettingsContent = File.ReadAllText(appsettings);
        Assert.DoesNotContain("Bearer", appsettingsContent);
        Assert.DoesNotContain("Scheme2", appsettingsContent);
    }

    [Fact]
    public void Key_CanResetSigningKey()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        app.Run(new[] { "key", "--project", project });
        Assert.Contains("Signing Key:", _console.GetOutput());

        app.Run(new[] { "key", "--reset", "--force", "--project", project });
        Assert.Contains("New signing key created:", _console.GetOutput());
    }

    [Fact]
    public async Task Key_CanResetSigningKey_WhenSecretsHasPrepulatedData()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(_fixture.TestSecretsId);
        await File.WriteAllTextAsync(secretsFilePath,
@"{
  ""Foo"": {
    ""Bar"": ""baz""
  }
}");

        app.Run(new[] { "create", "--project", project });
        app.Run(new[] { "key", "--project", project });
        Assert.Contains("Signing Key:", _console.GetOutput());

        app.Run(new[] { "key", "--reset", "--force", "--project", project });
        Assert.Contains("New signing key created:", _console.GetOutput());

        using FileStream openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.NotNull(secretsJson);
        Assert.True(secretsJson.ContainsKey(DevJwtsDefaults.SigningKeyConfigurationKey));
        Assert.True(secretsJson.TryGetPropertyValue("Foo", out var fooField));
        Assert.Equal("baz", fooField["Bar"].GetValue<string>());
    }

    [Fact]
    public void Command_ShowsHelpForInvalidCommand()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        var exception = Record.Exception(() => app.Run(new[] { "not-real", "--project", project }));

        Assert.Null(exception);
        Assert.Contains("Unrecognized command or argument 'not-real'", _console.GetOutput());
    }

    [Fact]
    public void CreateCommand_ShowsBasicTokenDetails()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var output = _console.GetOutput();

        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains("Token: ", output);
        Assert.DoesNotContain("Scheme", output);
    }

    [Fact]
    public void CreateCommand_SupportsODateTimeFormats()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--expires-on", DateTime.Now.AddDays(2).ToString("O") });
        var output = _console.GetOutput();

        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains("Token: ", output);
        Assert.Contains("Expires On", output);
        Assert.DoesNotContain("Scheme", output);
    }

    [Fact]
    public void CreateCommand_ShowsCustomizedTokenDetails()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--scheme", "customScheme" });
        var output = _console.GetOutput();

        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains("Token: ", output);
        Assert.Contains("Scheme: customScheme", output);
    }

    [Fact]
    public void CreateCommand_DisplaysErrorForInvalidExpiresOnCombination()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--expires-on", DateTime.UtcNow.AddDays(2).ToString("O"), "--valid-for", "2h" });
        var output = _console.GetOutput();

        Assert.Contains($"'--valid-for' and '--expires-on' are mutually exclusive flags. Provide either option but not both.", output);
        Assert.DoesNotContain("Expires On: ", output);
    }

    [Fact]
    public void PrintCommand_ShowsBasicOptions()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: Bearer", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
    }

    [Fact]
    public void PrintCommand_ShowsCustomizedOptions()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--role", "foobar" });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: Bearer", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
        Assert.Contains($"Roles: [foobar]", output);
        Assert.DoesNotContain("Custom Claims", output);
    }

    [Fact]
    public void PrintComamnd_ShowsAllOptionsWithShowAll()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--claim", "foo=bar" });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project, "--show-all" });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: Bearer", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
        Assert.Contains($"Scopes: none", output);
        Assert.Contains($"Roles: [none]", output);
        Assert.Contains($"Custom Claims: [foo=bar]", output);
    }

    [Fact]
    public void Create_WithJsonOutput_CanBeSerialized()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--output", "json" });
        var output = _console.GetOutput();
        var deserialized = JsonSerializer.Deserialize<Jwt>(output);

        Assert.NotNull(deserialized);
        Assert.Equal("Bearer", deserialized.Scheme);
        Assert.Equal(Environment.UserName, deserialized.Name);
    }

    [Fact]
    public void Create_WithTokenOutput_ProducesSingleValue()
    {
        var project = Path.Combine(_fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "-o", "token" });
        var output = _console.GetOutput();

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(output.Trim()));
    }

    [Fact]
    public void Create_GracefullyHandles_NoLaunchSettings()
    {
        var projectPath = _fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var app = new Program(_console);
        var launchSettingsPath = Path.Combine(projectPath, "Properties", "launchSettings.json");

        File.Delete(launchSettingsPath);

        app.Run(new[] { "create", "--project", project });
        var output = _console.GetOutput();

        Assert.Contains(Resources.CreateCommand_NoAudience_Error, output);
    }

    [Fact]
    public async Task Create_GracefullyHandles_PrepopulatedSecrets()
    {
        var projectPath = _fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(_fixture.TestSecretsId);
        await File.WriteAllTextAsync(secretsFilePath,
@"{
  ""Foo"": {
    ""Bar"": ""baz""
  }
}");
        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project});
        var output = _console.GetOutput();

        Assert.Contains("New JWT saved", output);
        using FileStream openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.NotNull(secretsJson);
        Assert.True(secretsJson.ContainsKey(DevJwtsDefaults.SigningKeyConfigurationKey));
        Assert.True(secretsJson.TryGetPropertyValue("Foo", out var fooField));
        Assert.Equal("baz", fooField["Bar"].GetValue<string>());
    }

    [Fact]
    public void Create_GetsAudiencesFromAllIISAndKestrel()
    {
        var projectPath = _fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(_fixture.TestSecretsId);

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project});
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        app.Run(new[] { "print", id, "--project", project, "--show-all" });
        var output = _console.GetOutput();

        Assert.Contains("New JWT saved", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
    }
}
