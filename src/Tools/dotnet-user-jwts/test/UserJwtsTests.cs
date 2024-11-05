// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.IdentityModel.Tokens.Jwt;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools.Tests;

public class UserJwtsTests(UserJwtsTestFixture fixture, ITestOutputHelper output) : IClassFixture<UserJwtsTestFixture>
{
    private readonly TestConsole _console = new(output);

    [Fact]
    public void List_NoTokensForNewProject()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "list", "--project", project });
        Assert.Contains("No JWTs created yet!", _console.GetOutput());
    }

    [Fact]
    public void List_HandlesNoSecretsInProject()
    {
        var project = Path.Combine(fixture.CreateProject(false), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "list", "--project", project });
        Assert.DoesNotContain("Set UserSecretsId to ", _console.GetOutput());
        Assert.Contains("No JWTs created yet!", _console.GetOutput());
    }

    [Fact]
    public void Create_CreatesSecretOnNoSecretInproject()
    {
        var project = Path.Combine(fixture.CreateProject(false), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var output = _console.GetOutput();
        Assert.DoesNotContain("could not find SecretManager.targets", output);
        Assert.DoesNotContain("Set UserSecretsId to ", output);
        Assert.Contains("New JWT saved", output);
    }

    [Fact]
    public void Create_WritesGeneratedTokenToDisk()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        Assert.Contains("New JWT saved", _console.GetOutput());
        Assert.Contains("dotnet-user-jwts", File.ReadAllText(appsettings));
    }

    [Fact]
    public void Create_CanModifyExistingScheme()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        Assert.Contains("New JWT saved", _console.GetOutput());

        var appSettings = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(appsettings));
        Assert.Equal("dotnet-user-jwts", appSettings["Authentication"]["Schemes"]["Bearer"]["ValidIssuer"].GetValue<string>());
        app.Run(["create", "--project", project, "--issuer", "new-issuer"]);
        appSettings = JsonSerializer.Deserialize<JsonObject>(File.ReadAllText(appsettings));
        Assert.Equal("new-issuer", appSettings["Authentication"]["Schemes"]["Bearer"]["ValidIssuer"].GetValue<string>());
    }

    [Fact]
    public void Print_ReturnsNothingForMissingToken()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "print", "invalid-id", "--project", project });
        Assert.Contains("No token with ID 'invalid-id' found", _console.GetOutput());
    }

    [Fact]
    public void List_ReturnsIdForGeneratedToken()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--scheme", "MyCustomScheme" });
        Assert.Contains("New JWT saved", _console.GetOutput());

        app.Run(new[] { "list", "--project", project });
        Assert.Contains("MyCustomScheme", _console.GetOutput());
    }

    [Fact]
    public void List_ReturnsIdForGeneratedToken_WithJsonFormat()
    {
        var schemeName = "MyCustomScheme";
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--scheme", schemeName });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        _console.ClearOutput();

        app.Run(new[] { "list", "--project", project, "--output", "json" });
        var output = _console.GetOutput();
        var deserialized = JsonSerializer.Deserialize<Dictionary<string, Jwt>>(output);

        var jwt = deserialized[id];

        Assert.NotNull(deserialized);
        Assert.Equal(schemeName, jwt.Scheme);
    }

    [Fact]
    public void List_ReturnsEmptyListWhenNoTokens_WithJsonFormat()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "list", "--project", project, "--output", "json" });
        var output = _console.GetOutput();

        Assert.Equal("[]", output.Trim());
    }

    [Fact]
    public void Remove_RemovesGeneratedToken()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        app.Run(new[] { "create", "--project", project, "--scheme", "Scheme2" });

        app.Run(new[] { "remove", id, "--project", project });
        var appsettingsContent = File.ReadAllText(appsettings);
        Assert.DoesNotContain(DevJwtsDefaults.Scheme, appsettingsContent);
        Assert.Contains("Scheme2", appsettingsContent);
    }

    [Fact]
    public void Clear_RemovesGeneratedTokens()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var appsettings = Path.Combine(Path.GetDirectoryName(project), "appsettings.Development.json");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        app.Run(new[] { "create", "--project", project, "--scheme", "Scheme2" });

        Assert.Contains("New JWT saved", _console.GetOutput());

        app.Run(new[] { "clear", "--project", project, "--force" });
        var appsettingsContent = File.ReadAllText(appsettings);
        Assert.DoesNotContain(DevJwtsDefaults.Scheme, appsettingsContent);
        Assert.DoesNotContain("Scheme2", appsettingsContent);
    }

    [Fact]
    public void Key_CanResetSigningKey()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
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
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);
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

        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.NotNull(secretsJson);
        Assert.True(secretsJson.ContainsKey(SigningKeysHandler.GetSigningKeyPropertyName(DevJwtsDefaults.Scheme)));
        Assert.True(secretsJson.TryGetPropertyValue("Foo", out var fooField));
        Assert.Equal("baz", fooField["Bar"].GetValue<string>());
    }

    [Fact]
    public void Command_ShowsHelpForInvalidCommand()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        var exception = Record.Exception(() => app.Run(new[] { "not-real", "--project", project }));

        Assert.Null(exception);
        Assert.Contains("Unrecognized command or argument 'not-real'", _console.GetOutput());
    }

    [Fact]
    public void CreateCommand_ShowsBasicTokenDetails()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
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
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
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
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
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
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--expires-on", DateTime.UtcNow.AddDays(2).ToString("O"), "--valid-for", "2h" });
        var output = _console.GetOutput();

        Assert.Contains($"'--valid-for' and '--expires-on' are mutually exclusive flags. Provide either option but not both.", output);
        Assert.DoesNotContain("Expires On: ", output);
    }

    [Fact]
    public void PrintCommand_ShowsBasicOptions()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: {DevJwtsDefaults.Scheme}", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
    }

    [Fact]
    public void PrintCommand_ShowsBasicOptions_WithJsonFormat()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        _console.ClearOutput();

        app.Run(new[] { "print", id, "--project", project, "--output", "json" });
        var output = _console.GetOutput();
        var deserialized = JsonSerializer.Deserialize<Jwt>(output);

        Assert.Equal(Environment.UserName, deserialized.Name);
        Assert.Equal(DevJwtsDefaults.Scheme, deserialized.Scheme);
        Assert.Equal($"http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", deserialized.Audience);
    }

    [Fact]
    public void PrintCommand_ShowsCustomizedOptions()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--role", "foobar" });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: {DevJwtsDefaults.Scheme}", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
        Assert.Contains($"Roles: [foobar]", output);
        Assert.DoesNotContain("Custom Claims", output);
    }

    [Fact]
    public void PrintComamnd_ShowsAllOptionsWithShowAll()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--claim", "foo=bar" });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;

        app.Run(new[] { "print", id, "--project", project, "--show-all" });
        var output = _console.GetOutput();

        Assert.Contains($"ID: {id}", output);
        Assert.Contains($"Name: {Environment.UserName}", output);
        Assert.Contains($"Scheme: {DevJwtsDefaults.Scheme}", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
        Assert.Contains($"Scopes: none", output);
        Assert.Contains($"Roles: [none]", output);
        Assert.Contains($"Custom Claims: [foo=bar]", output);
    }

    [Fact]
    public void Create_WithJsonOutput_CanBeSerialized()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "--output", "json" });
        var output = _console.GetOutput();
        var deserialized = JsonSerializer.Deserialize<Jwt>(output);

        Assert.NotNull(deserialized);
        Assert.Equal(DevJwtsDefaults.Scheme, deserialized.Scheme);
        Assert.Equal(Environment.UserName, deserialized.Name);
    }

    [Fact]
    public void Create_WithTokenOutput_ProducesSingleValue()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project, "-o", "token" });
        var output = _console.GetOutput();

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(output.Trim()));
    }

    [Fact]
    public void Create_GracefullyHandles_NoLaunchSettings()
    {
        var projectPath = fixture.CreateProject();
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
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);
        await File.WriteAllTextAsync(secretsFilePath,
@"{
  ""Foo"": {
    ""Bar"": ""baz""
  }
}");
        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project });
        var output = _console.GetOutput();

        Assert.Contains("New JWT saved", output);
        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.NotNull(secretsJson);
        var signingKey = Assert.Single(secretsJson[SigningKeysHandler.GetSigningKeyPropertyName(DevJwtsDefaults.Scheme)].AsArray());
        Assert.Equal(32, signingKey["Length"].GetValue<int>());
        Assert.True(Convert.TryFromBase64String(signingKey["Value"].GetValue<string>(), new byte[32], out var _));
        Assert.True(secretsJson.TryGetPropertyValue("Foo", out var fooField));
        Assert.Equal("baz", fooField["Bar"].GetValue<string>());
    }

    [Fact]
    public async Task Create_GracefullyHandles_PrepopulatedSecrets_WithCommasAndComments()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);
        await File.WriteAllTextAsync(secretsFilePath,
@"{
  ""Foo"": {
    ""Bar"": ""baz"",
    //""Bar"": ""baz"",
  }
}");
        var app = new Program(_console);
        app.Run(["create", "--project", project]);
        var output = _console.GetOutput();

        Assert.Contains("New JWT saved", output);
        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.NotNull(secretsJson);
        var signingKey = Assert.Single(secretsJson[SigningKeysHandler.GetSigningKeyPropertyName(DevJwtsDefaults.Scheme)].AsArray());
        Assert.Equal(32, signingKey["Length"].GetValue<int>());
        Assert.True(Convert.TryFromBase64String(signingKey["Value"].GetValue<string>(), new byte[32], out var _));
        Assert.True(secretsJson.TryGetPropertyValue("Foo", out var fooField));
        Assert.Equal("baz", fooField["Bar"].GetValue<string>());
    }

    [Fact]
    public void Create_GetsAudiencesFromAllIISAndKestrel()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project });
        var matches = Regex.Matches(_console.GetOutput(), "New JWT saved with ID '(.*?)'");
        var id = matches.SingleOrDefault().Groups[1].Value;
        app.Run(new[] { "print", id, "--project", project, "--show-all" });
        var output = _console.GetOutput();

        Assert.Contains("New JWT saved", output);
        Assert.Contains($"Audience(s): http://localhost:23528, https://localhost:44395, https://localhost:5001, http://localhost:5000", output);
    }

    [Fact]
    public async Task Create_SupportsSettingACustomIssuerAndScheme()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme" });

        Assert.Contains("New JWT saved", _console.GetOutput());

        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.True(secretsJson.ContainsKey(SigningKeysHandler.GetSigningKeyPropertyName("test-scheme")));
        var signingKey = Assert.Single(secretsJson[SigningKeysHandler.GetSigningKeyPropertyName("test-scheme")].AsArray());
        Assert.Equal(32, signingKey["Length"].GetValue<int>());
        Assert.True(Convert.TryFromBase64String(signingKey["Value"].GetValue<string>(), new byte[32], out var _));
        Assert.Equal("test-issuer", signingKey["Issuer"].GetValue<string>());
    }

    [Fact]
    public async Task Create_SupportsSettingMutlipleIssuersAndSingleScheme()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer-2", "--scheme", "test-scheme" });

        Assert.Contains("New JWT saved", _console.GetOutput());

        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        Assert.True(secretsJson.ContainsKey(SigningKeysHandler.GetSigningKeyPropertyName("test-scheme")));
        var signingKeys = secretsJson[SigningKeysHandler.GetSigningKeyPropertyName("test-scheme")].AsArray();
        Assert.Equal(2, signingKeys.Count);
        Assert.NotNull(signingKeys.SingleOrDefault(signingKey => signingKey["Issuer"].GetValue<string>() == "test-issuer"));
        Assert.NotNull(signingKeys.SingleOrDefault(signingKey => signingKey["Issuer"].GetValue<string>() == "test-issuer-2"));
    }

    [Fact]
    public async Task Create_SupportsSettingSingleIssuerAndMultipleSchemes()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretsFilePath = PathHelper.GetSecretsPathFromSecretsId(fixture.TestSecretsId);

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme-2" });

        Assert.Contains("New JWT saved", _console.GetOutput());

        using var openStream = File.OpenRead(secretsFilePath);
        var secretsJson = await JsonSerializer.DeserializeAsync<JsonObject>(openStream);
        var signingKey1 = Assert.Single(secretsJson[SigningKeysHandler.GetSigningKeyPropertyName("test-scheme")].AsArray());
        Assert.Equal("test-issuer", signingKey1["Issuer"].GetValue<string>());
        Assert.Equal(32, signingKey1["Length"].GetValue<int>());
        Assert.True(Convert.TryFromBase64String(signingKey1["Value"].GetValue<string>(), new byte[32], out var _));
        var signingKey2 = Assert.Single(secretsJson[SigningKeysHandler.GetSigningKeyPropertyName("test-scheme-2")].AsArray());
        Assert.Equal("test-issuer", signingKey2["Issuer"].GetValue<string>());
        Assert.Equal(32, signingKey2["Length"].GetValue<int>());
        Assert.True(Convert.TryFromBase64String(signingKey2["Value"].GetValue<string>(), new byte[32], out var _));
    }

    [Fact]
    public void Key_CanPrintAndReset_BySchemeAndIssuer()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme-2" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer-2", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer-2", "--scheme", "test-scheme-3" });

        Assert.Contains("New JWT saved", _console.GetOutput());
        _console.ClearOutput();

        app.Run(new[] { "key", "--project", project, "--scheme", "test-scheme", "--issuer", "test-issuer" });
        var printMatches = Regex.Matches(_console.GetOutput(), "Signing Key: '(.*?)'");
        var key = printMatches.SingleOrDefault().Groups[1].Value;
        _console.ClearOutput();

        app.Run(new[] { "key", "--project", project, "--reset", "--force", "--scheme", "test-scheme", "--issuer", "test-issuer" });
        var resetMatches = Regex.Matches(_console.GetOutput(), "New signing key created: '(.*?)'");
        var resetKey = resetMatches.SingleOrDefault().Groups[1].Value;
        Assert.NotEqual(key, resetKey);
    }

    [Fact]
    public void Key_CanPrintWithBase64()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer", "--scheme", "test-scheme-2" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer-2", "--scheme", "test-scheme" });
        app.Run(new[] { "create", "--project", project, "--issuer", "test-issuer-2", "--scheme", "test-scheme-3" });

        Assert.Contains("New JWT saved", _console.GetOutput());
        _console.ClearOutput();

        app.Run(new[] { "key", "--project", project, "--scheme", "test-scheme", "--issuer", "test-issuer" });
        var printMatches = Regex.Matches(_console.GetOutput(), "Signing Key: '(.*?)'");
        var key = printMatches.SingleOrDefault().Groups[1].Value;
        _console.ClearOutput();

        var buffer = new Span<byte>(new byte[key.Length]);
        Assert.True(Convert.TryFromBase64String(key, buffer, out var bytesParsed));
        Assert.Equal(32, bytesParsed);
    }

    [Fact]
    public void Create_CanHandleNoProjectOptionProvided()
    {
        var projectPath = fixture.CreateProject();
        Directory.SetCurrentDirectory(projectPath);

        var app = new Program(_console);
        app.Run(["create"]);

        Assert.DoesNotContain("No project found at `-p|--project` path or current directory.", _console.GetOutput());
        Assert.Contains("New JWT saved", _console.GetOutput());
    }

    [Fact]
    public void Create_CanHandleNoProjectOptionProvided_WithNoProjects()
    {
        var path = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "userjwtstest"));
        Directory.SetCurrentDirectory(path.FullName);

        var app = new Program(_console);
        app.Run(["create"]);

        Assert.Contains($"Could not find a MSBuild project file in '{Directory.GetCurrentDirectory()}'. Specify which project to use with the --project option.", _console.GetOutput());
        Assert.DoesNotContain(Resources.CreateCommand_NoAudience_Error, _console.GetOutput());
    }

    [Fact]
    public void Delete_CanHandleNoProjectOptionProvided_WithNoProjects()
    {
        var path = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "userjwtstest"));
        Directory.SetCurrentDirectory(path.FullName);

        var app = new Program(_console);
        app.Run(["remove", "some-id"]);

        Assert.Contains($"Could not find a MSBuild project file in '{Directory.GetCurrentDirectory()}'. Specify which project to use with the --project option.", _console.GetOutput());
    }

    [Fact]
    public void Clear_CanHandleNoProjectOptionProvided_WithNoProjects()
    {
        var path = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "userjwtstest"));
        Directory.SetCurrentDirectory(path.FullName);

        var app = new Program(_console);
        app.Run(["clear"]);

        Assert.Contains($"Could not find a MSBuild project file in '{Directory.GetCurrentDirectory()}'. Specify which project to use with the --project option.", _console.GetOutput());
    }

    [Fact]
    public void List_CanHandleNoProjectOptionProvided_WithNoProjects()
    {
        var path = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "userjwtstest"));
        Directory.SetCurrentDirectory(path.FullName);

        var app = new Program(_console);
        app.Run(["list"]);

        Assert.Contains($"Could not find a MSBuild project file in '{Directory.GetCurrentDirectory()}'. Specify which project to use with the --project option.", _console.GetOutput());
    }

    [Fact]
    public void List_CanHandleProjectOptionAsPath()
    {
        var projectPath = fixture.CreateProject();
        var project = Path.Combine(projectPath, "TestProject.csproj");

        var app = new Program(_console);
        app.Run(new[] { "list", "--project", projectPath });

        Assert.Contains(Path.Combine(projectPath, project), _console.GetOutput());
    }

    [Fact]
    public void List_CanHandleRelativePathAsOption()
    {
        var projectPath = fixture.CreateProject();
        var tempPath = Path.GetTempPath();
        var targetPath = Path.GetRelativePath(tempPath, projectPath);
        var project = Path.Combine(projectPath, "TestProject.csproj");
        Directory.SetCurrentDirectory(tempPath);

        var app = new Program(_console);
        app.Run(new[] { "list", "--project", targetPath });

        Assert.DoesNotContain($"The project file '{targetPath}' does not exist.", _console.GetOutput());
        Assert.Contains(Path.Combine(projectPath, project), _console.GetOutput());
    }

    [Fact]
    public void Create_CanHandleRelativePathAsOption()
    {
        var projectPath = fixture.CreateProject();
        var tempPath = Path.GetTempPath();
        var targetPath = Path.GetRelativePath(tempPath, projectPath);
        Directory.SetCurrentDirectory(tempPath);

        var app = new Program(_console);
        app.Run(new[] { "create", "--project", targetPath });

        Assert.DoesNotContain($"The project file '{targetPath}' does not exist.", _console.GetOutput());
        Assert.Contains("New JWT saved", _console.GetOutput());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    public void Create_CreatesFileWithUserOnlyUnixFileMode()
    {
        var project = Path.Combine(fixture.CreateProject(), "TestProject.csproj");
        var app = new Program(_console);

        app.Run(new[] { "create", "--project", project });

        Assert.Contains("New JWT saved", _console.GetOutput());

        Assert.NotNull(app.UserJwtsFilePath);
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(app.UserJwtsFilePath));
    }
}
