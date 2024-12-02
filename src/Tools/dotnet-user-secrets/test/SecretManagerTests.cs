// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Tests;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

public class SecretManagerTests : IClassFixture<UserSecretsTestFixture>
{
    private readonly TestConsole _console;
    private readonly UserSecretsTestFixture _fixture;
    private readonly ITestOutputHelper _testOut;

    public SecretManagerTests(UserSecretsTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;

        _testOut = output;

        _console = new TestConsole(output);
    }

    private Program CreateProgram()
    {
        return new Program(_console, Directory.GetCurrentDirectory());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Error_MissingId(string id)
    {
        var project = Path.Combine(_fixture.CreateProject(id), "TestProject.csproj");
        var secretManager = CreateProgram();

        secretManager.RunInternal("list", "-p", project, "--verbose");
        Assert.Contains(Resources.FormatError_ProjectMissingId(project), _console.GetOutput());
    }

    [Fact]
    public void Error_InvalidProjectFormat()
    {
        var project = Path.Combine(_fixture.CreateProject("<"), "TestProject.csproj");
        var secretManager = CreateProgram();

        secretManager.RunInternal("list", "-p", project);
        Assert.Contains(Resources.FormatError_ProjectFailedToLoad(project), _console.GetOutput());
    }

    [Fact]
    public void Error_Project_DoesNotExist()
    {
        var projectPath = Path.Combine(_fixture.GetTempSecretProject(), "does_not_exist", "TestProject.csproj");
        var secretManager = CreateProgram();

        secretManager.RunInternal("list", "--project", projectPath);
        Assert.Contains(Resources.FormatError_ProjectPath_NotFound(projectPath), _console.GetOutput());
    }

    [Fact]
    public void SupportsRelativePaths()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var cwd = Path.Combine(projectPath, "nested1");
        Directory.CreateDirectory(cwd);
        var secretManager = new Program(_console, cwd);

        secretManager.RunInternal("list", "-p", ".." + Path.DirectorySeparatorChar, "--verbose");

        Assert.Contains(Resources.FormatMessage_Project_File_Path(Path.Combine(cwd, "..", "TestProject.csproj")), _console.GetOutput());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetSecrets(bool fromCurrentDirectory)
    {
        var secrets = new KeyValuePair<string, string>[]
                    {
                        new KeyValuePair<string, string>("key1", Guid.NewGuid().ToString()),
                        new KeyValuePair<string, string>("Facebook:AppId", Guid.NewGuid().ToString()),
                        new KeyValuePair<string, string>(@"key-@\/.~123!#$%^&*())-+==", @"key-@\/.~123!#$%^&*())-+=="),
                        new KeyValuePair<string, string>("key2", string.Empty),
                        new KeyValuePair<string, string>("-oneDashedKey", "-oneDashedValue"),
                        new KeyValuePair<string, string>("--twoDashedKey", "--twoDashedValue")
                    };

        var projectPath = _fixture.GetTempSecretProject();
        var dir = fromCurrentDirectory
            ? projectPath
            : Path.GetTempPath();
        var secretManager = new Program(_console, dir);

        foreach (var secret in secrets)
        {
            var parameters = fromCurrentDirectory ?
                new string[] { "set", secret.Key, secret.Value, "--verbose" } :
                new string[] { "set", secret.Key, secret.Value, "-p", projectPath, "--verbose" };
            secretManager.RunInternal(parameters);
        }

        foreach (var keyValue in secrets)
        {
            Assert.Contains(
                string.Format(CultureInfo.InvariantCulture, "Successfully saved {0} to the secret store.", keyValue.Key),
                _console.GetOutput());
        }

        _console.ClearOutput();
        var args = fromCurrentDirectory
            ? new string[] { "list", "--verbose" }
            : new string[] { "list", "-p", projectPath, "--verbose" };
        secretManager.RunInternal(args);
        foreach (var keyValue in secrets)
        {
            Assert.Contains(
                string.Format(CultureInfo.InvariantCulture, "{0} = {1}", keyValue.Key, keyValue.Value),
                _console.GetOutput());
        }

        // Remove secrets.
        _console.ClearOutput();
        foreach (var secret in secrets)
        {
            var parameters = fromCurrentDirectory ?
                new string[] { "remove", secret.Key, "--verbose" } :
                new string[] { "remove", secret.Key, "-p", projectPath, "--verbose" };
            secretManager.RunInternal(parameters);
        }

        // Verify secrets are removed.
        _console.ClearOutput();
        args = fromCurrentDirectory
            ? new string[] { "list", "--verbose" }
            : new string[] { "list", "-p", projectPath, "--verbose" };
        secretManager.RunInternal(args);
        Assert.Contains(Resources.Error_No_Secrets_Found, _console.GetOutput());
    }

    [Fact]
    public void SetSecret_Update_Existing_Secret()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var secretManager = CreateProgram();

        secretManager.RunInternal("set", "secret1", "value1", "-p", projectPath, "--verbose");
        Assert.Contains("Successfully saved secret1 to the secret store.", _console.GetOutput());
        secretManager.RunInternal("set", "secret1", "value2", "-p", projectPath, "--verbose");
        Assert.Contains("Successfully saved secret1 to the secret store.", _console.GetOutput());

        _console.ClearOutput();

        secretManager.RunInternal("list", "-p", projectPath, "--verbose");
        Assert.Contains("secret1 = value2", _console.GetOutput());
    }

    [Fact]
    public void SetSecret_With_Verbose_Flag()
    {
        string secretId;
        var projectPath = _fixture.GetTempSecretProject(out secretId);
        var secretManager = CreateProgram();

        secretManager.RunInternal("-v", "set", "secret1", "value1", "-p", projectPath);
        Assert.Contains(string.Format(CultureInfo.InvariantCulture, "Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _console.GetOutput());
        Assert.Contains(string.Format(CultureInfo.InvariantCulture, "Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _console.GetOutput());
        Assert.Contains("Successfully saved secret1 to the secret store.", _console.GetOutput());
        _console.ClearOutput();

        secretManager.RunInternal("-v", "list", "-p", projectPath);

        Assert.Contains(string.Format(CultureInfo.InvariantCulture, "Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _console.GetOutput());
        Assert.Contains(string.Format(CultureInfo.InvariantCulture, "Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _console.GetOutput());
        Assert.Contains("secret1 = value1", _console.GetOutput());
    }

    [Fact]
    public void Remove_Non_Existing_Secret()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var secretManager = CreateProgram();
        secretManager.RunInternal("remove", "secret1", "-p", projectPath, "--verbose");
        Assert.Contains("Cannot find 'secret1' in the secret store.", _console.GetOutput());
    }

    [Fact]
    public void Remove_Is_Case_Insensitive()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var secretManager = CreateProgram();
        secretManager.RunInternal("set", "SeCreT1", "value", "-p", projectPath, "--verbose");
        secretManager.RunInternal("list", "-p", projectPath, "--verbose");
        Assert.Contains("SeCreT1 = value", _console.GetOutput());
        secretManager.RunInternal("remove", "secret1", "-p", projectPath, "--verbose");

        _console.ClearOutput();
        secretManager.RunInternal("list", "-p", projectPath, "--verbose");

        Assert.Contains(Resources.Error_No_Secrets_Found, _console.GetOutput());
    }

    [Fact]
    public void List_Flattens_Nested_Objects()
    {
        string secretId;
        var projectPath = _fixture.GetTempSecretProject(out secretId);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(secretId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFile));
        File.WriteAllText(secretsFile, @"{ ""AzureAd"": { ""ClientSecret"": ""abcdéƒ©˙î""} }", Encoding.UTF8);
        var secretManager = CreateProgram();
        secretManager.RunInternal("list", "-p", projectPath, "--verbose");
        Assert.Contains("AzureAd:ClientSecret = abcdéƒ©˙î", _console.GetOutput());
    }

    [Fact]
    public void List_Json()
    {
        string id;
        var projectPath = _fixture.GetTempSecretProject(out id);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(id);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFile));
        File.WriteAllText(secretsFile, @"{ ""AzureAd"": { ""ClientSecret"": ""abcdéƒ©˙î""} }", Encoding.UTF8);
        var secretManager = new Program(_console, Path.GetDirectoryName(projectPath));
        secretManager.RunInternal("list", "--id", id, "--json");
        var stdout = _console.GetOutput();
        Assert.Contains("//BEGIN", stdout);
        Assert.Contains(@"""AzureAd:ClientSecret"": ""abcdéƒ©˙î""", stdout);
        Assert.Contains("//END", stdout);
    }

    [Fact]
    public void Set_Flattens_Nested_Objects()
    {
        string secretId;
        var projectPath = _fixture.GetTempSecretProject(out secretId);
        var secretsFile = PathHelper.GetSecretsPathFromSecretsId(secretId);
        Directory.CreateDirectory(Path.GetDirectoryName(secretsFile));
        File.WriteAllText(secretsFile, @"{ ""AzureAd"": { ""ClientSecret"": ""abcdéƒ©˙î""} }", Encoding.UTF8);
        var secretManager = CreateProgram();
        secretManager.RunInternal("set", "AzureAd:ClientSecret", "¡™£¢∞", "-p", projectPath);
        secretManager.RunInternal("list", "-p", projectPath);

        Assert.Contains("AzureAd:ClientSecret = ¡™£¢∞", _console.GetOutput());
        var fileContents = File.ReadAllText(secretsFile, Encoding.UTF8);
        Assert.Equal(@"{
    ""AzureAd:ClientSecret"": ""¡™£¢∞""
}",
            fileContents, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
    }

    [Fact]
    public void List_Empty_Secrets_File()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var secretManager = CreateProgram();
        secretManager.RunInternal("list", "-p", projectPath, "--verbose");
        Assert.Contains(Resources.Error_No_Secrets_Found, _console.GetOutput());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Clear_Secrets(bool fromCurrentDirectory)
    {
        var projectPath = _fixture.GetTempSecretProject();

        var dir = fromCurrentDirectory
            ? projectPath
            : Path.GetTempPath();

        var secretManager = new Program(_console, dir);

        var secrets = new KeyValuePair<string, string>[]
                    {
                            new KeyValuePair<string, string>("key1", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>("Facebook:AppId", Guid.NewGuid().ToString()),
                            new KeyValuePair<string, string>(@"key-@\/.~123!#$%^&*())-+==", @"key-@\/.~123!#$%^&*())-+=="),
                            new KeyValuePair<string, string>("key2", string.Empty)
                    };

        foreach (var secret in secrets)
        {
            var parameters = fromCurrentDirectory ?
                new string[] { "set", secret.Key, secret.Value, "--verbose" } :
                new string[] { "set", secret.Key, secret.Value, "-p", projectPath, "--verbose" };
            secretManager.RunInternal(parameters);
        }

        foreach (var keyValue in secrets)
        {
            Assert.Contains(
                string.Format(CultureInfo.InvariantCulture, "Successfully saved {0} to the secret store.", keyValue.Key),
                _console.GetOutput());
        }

        // Verify secrets are persisted.
        _console.ClearOutput();
        var args = fromCurrentDirectory ?
            new string[] { "list", "--verbose" } :
            new string[] { "list", "-p", projectPath, "--verbose" };
        secretManager.RunInternal(args);
        foreach (var keyValue in secrets)
        {
            Assert.Contains(
                string.Format(CultureInfo.InvariantCulture, "{0} = {1}", keyValue.Key, keyValue.Value),
                _console.GetOutput());
        }

        // Clear secrets.
        _console.ClearOutput();
        args = fromCurrentDirectory ? new string[] { "clear", "--verbose" } : new string[] { "clear", "-p", projectPath, "--verbose" };
        secretManager.RunInternal(args);

        args = fromCurrentDirectory ? new string[] { "list", "--verbose" } : new string[] { "list", "-p", projectPath, "--verbose" };
        secretManager.RunInternal(args);
        Assert.Contains(Resources.Error_No_Secrets_Found, _console.GetOutput());
    }

    [Fact]
    public void Init_When_Project_Has_No_Secrets_Id()
    {
        var projectPath = _fixture.CreateProject(null);
        var project = Path.Combine(projectPath, "TestProject.csproj");
        var secretManager = new Program(_console, projectPath);

        secretManager.RunInternal("init", "-p", project);

        Assert.DoesNotContain(Resources.FormatError_ProjectMissingId(project), _console.GetOutput());
        Assert.DoesNotContain("--help", _console.GetOutput());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "UnixFileMode is not supported on Windows.")]
    public void SetSecrets_CreatesFileWithUserOnlyUnixFileMode()
    {
        var projectPath = _fixture.GetTempSecretProject();
        var secretManager = new Program(_console, projectPath);

        secretManager.RunInternal("set", "key1", Guid.NewGuid().ToString(), "--verbose");

        Assert.NotNull(secretManager.SecretsFilePath);
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(secretManager.SecretsFilePath));
    }
}
