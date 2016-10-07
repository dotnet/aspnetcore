// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Tests;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests
{
    public class SecretManagerTests : IClassFixture<UserSecretsTestFixture>
    {
        private TestLogger _logger;
        private readonly UserSecretsTestFixture _fixture;

        public SecretManagerTests(UserSecretsTestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _logger = new TestLogger(output);

        }

        private Program CreateProgram()
        {
            return new Program(new TestConsole(), Directory.GetCurrentDirectory())
            {
                Logger = _logger
            };
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Error_MissingId(string id)
        {
            var project = Path.Combine(_fixture.CreateProject(id), "TestProject.csproj");
            var secretManager = CreateProgram();

            var ex = Assert.Throws<GracefulException>(() => secretManager.RunInternal("list", "-p", project));
            Assert.Equal(Resources.FormatError_ProjectMissingId(project), ex.Message);
        }

        [Fact]
        public void Error_InvalidProjectFormat()
        {
            var project = Path.Combine(_fixture.CreateProject("<"), "TestProject.csproj");
            var secretManager = CreateProgram();

            var ex = Assert.Throws<GracefulException>(() => secretManager.RunInternal("list", "-p", project));
            Assert.Equal(Resources.FormatError_ProjectFailedToLoad(project), ex.Message);
        }

        [Fact]
        public void Error_Project_DoesNotExist()
        {
            var projectPath = Path.Combine(_fixture.GetTempSecretProject(), "does_not_exist", "TestProject.csproj");
            var secretManager = CreateProgram();

            var ex = Assert.Throws<GracefulException>(() => secretManager.RunInternal("list", "--project", projectPath));
            Assert.Equal(Resources.FormatError_ProjectPath_NotFound(projectPath), ex.Message);
        }

        [Fact]
        public void SupportsRelativePaths()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var cwd = Path.Combine(projectPath, "nested1");
            Directory.CreateDirectory(cwd);
            var secretManager = new Program(new TestConsole(), cwd) { Logger = _logger, CommandOutputProvider = _logger.CommandOutputProvider };
            secretManager.CommandOutputProvider.LogLevel = LogLevel.Debug;

            secretManager.RunInternal("list", "-p", ".." + Path.DirectorySeparatorChar, "--verbose");

            Assert.Contains(Resources.FormatMessage_Project_File_Path(Path.Combine(cwd, "..", "TestProject.csproj")), _logger.Messages);
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
                            new KeyValuePair<string, string>("key2", string.Empty)
                        };

            var projectPath = _fixture.GetTempSecretProject();
            var dir = fromCurrentDirectory
                ? projectPath
                : Path.GetTempPath();
            var secretManager = new Program(new TestConsole(), dir) { Logger = _logger };

            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            Assert.Equal(4, _logger.Messages.Count);

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    _logger.Messages);
            }

            _logger.Messages.Clear();
            var args = fromCurrentDirectory
                ? new string[] { "list" }
                : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Equal(4, _logger.Messages.Count);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    _logger.Messages);
            }

            // Remove secrets.
            _logger.Messages.Clear();
            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "remove", secret.Key } :
                    new string[] { "remove", secret.Key, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            // Verify secrets are removed.
            _logger.Messages.Clear();
            args = fromCurrentDirectory
                ? new string[] { "list" }
                : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, _logger.Messages);
        }

        [Fact]
        public void SetSecret_Update_Existing_Secret()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();

            secretManager.RunInternal("set", "secret1", "value1", "-p", projectPath);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", _logger.Messages);
            secretManager.RunInternal("set", "secret1", "value2", "-p", projectPath);
            Assert.Equal(2, _logger.Messages.Count);
            Assert.Contains("Successfully saved secret1 = value2 to the secret store.", _logger.Messages);

            _logger.Messages.Clear();

            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains("secret1 = value2", _logger.Messages);
        }

        [Fact]
        public void SetSecret_With_Verbose_Flag()
        {
            string secretId;
            var projectPath = _fixture.GetTempSecretProject(out secretId);
            _logger.SetLevel(LogLevel.Debug);
            var secretManager = CreateProgram();

            secretManager.RunInternal("-v", "set", "secret1", "value1", "-p", projectPath);
            Assert.Equal(3, _logger.Messages.Count);
            Assert.Contains(string.Format("Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _logger.Messages);
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _logger.Messages);
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", _logger.Messages);
            _logger.Messages.Clear();

            secretManager.RunInternal("-v", "list", "-p", projectPath);

            Assert.Equal(3, _logger.Messages.Count);
            Assert.Contains(string.Format("Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _logger.Messages);
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _logger.Messages);
            Assert.Contains("secret1 = value1", _logger.Messages);
        }

        [Fact]
        public void Remove_Non_Existing_Secret()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();
            secretManager.RunInternal("remove", "secret1", "-p", projectPath);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains("Cannot find 'secret1' in the secret store.", _logger.Messages);
        }

        [Fact]
        public void Remove_Is_Case_Insensitive()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();
            secretManager.RunInternal("set", "SeCreT1", "value", "-p", projectPath);
            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Contains("SeCreT1 = value", _logger.Messages);
            secretManager.RunInternal("remove", "secret1", "-p", projectPath);

            Assert.Equal(2, _logger.Messages.Count);
            _logger.Messages.Clear();
            secretManager.RunInternal("list", "-p", projectPath);

            Assert.Contains(Resources.Error_No_Secrets_Found, _logger.Messages);
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
            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains("AzureAd:ClientSecret = abcdéƒ©˙î", _logger.Messages);
        }

        [Fact]
        public void List_Json()
        {
            var output = new StringBuilder();
            var testConsole = new TestConsole
            {
                Out = new StringWriter(output)
            };
            string id;
            var projectPath = _fixture.GetTempSecretProject(out id);
            var secretsFile = PathHelper.GetSecretsPathFromSecretsId(id);
            Directory.CreateDirectory(Path.GetDirectoryName(secretsFile));
            File.WriteAllText(secretsFile, @"{ ""AzureAd"": { ""ClientSecret"": ""abcdéƒ©˙î""} }", Encoding.UTF8);
            var secretManager = new Program(testConsole, Path.GetDirectoryName(projectPath)) { Logger = _logger };
            secretManager.RunInternal("list", "--id", id, "--json");
            var stdout = output.ToString();
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
            Assert.Equal(1, _logger.Messages.Count);
            secretManager.RunInternal("list", "-p", projectPath);

            Assert.Equal(2, _logger.Messages.Count);
            Assert.Contains("AzureAd:ClientSecret = ¡™£¢∞", _logger.Messages);
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
            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, _logger.Messages);
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

            var secretManager = new Program(new TestConsole(), dir) { Logger = _logger };

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
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            Assert.Equal(4, _logger.Messages.Count);

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    _logger.Messages);
            }

            // Verify secrets are persisted.
            _logger.Messages.Clear();
            var args = fromCurrentDirectory ?
                new string[] { "list" } :
                new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Equal(4, _logger.Messages.Count);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    _logger.Messages);
            }

            // Clear secrets.
            _logger.Messages.Clear();
            args = fromCurrentDirectory ? new string[] { "clear" } : new string[] { "clear", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Equal(0, _logger.Messages.Count);

            args = fromCurrentDirectory ? new string[] { "list" } : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Equal(1, _logger.Messages.Count);
            Assert.Contains(Resources.Error_No_Secrets_Found, _logger.Messages);
        }
    }
}