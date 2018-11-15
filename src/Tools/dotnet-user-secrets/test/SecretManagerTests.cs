// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.UserSecrets.Tests;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests
{
    public class SecretManagerTests : IClassFixture<UserSecretsTestFixture>
    {
        private readonly TestConsole _console;
        private readonly UserSecretsTestFixture _fixture;
        private readonly StringBuilder _output = new StringBuilder();

        public SecretManagerTests(UserSecretsTestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;

            _console = new TestConsole(output)
            {
                Error = new StringWriter(_output),
                Out = new StringWriter(_output),
            };
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

            secretManager.RunInternal("list", "-p", project);
            Assert.Contains(Resources.FormatError_ProjectMissingId(project), _output.ToString());
        }

        [Fact]
        public void Error_InvalidProjectFormat()
        {
            var project = Path.Combine(_fixture.CreateProject("<"), "TestProject.csproj");
            var secretManager = CreateProgram();

            secretManager.RunInternal("list", "-p", project);
            Assert.Contains(Resources.FormatError_ProjectFailedToLoad(project), _output.ToString());
        }

        [Fact]
        public void Error_Project_DoesNotExist()
        {
            var projectPath = Path.Combine(_fixture.GetTempSecretProject(), "does_not_exist", "TestProject.csproj");
            var secretManager = CreateProgram();

            secretManager.RunInternal("list", "--project", projectPath);
            Assert.Contains(Resources.FormatError_ProjectPath_NotFound(projectPath), _output.ToString());
        }

        [Fact]
        public void SupportsRelativePaths()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var cwd = Path.Combine(projectPath, "nested1");
            Directory.CreateDirectory(cwd);
            var secretManager = new Program(_console, cwd);

            secretManager.RunInternal("list", "-p", ".." + Path.DirectorySeparatorChar, "--verbose");

            Assert.Contains(Resources.FormatMessage_Project_File_Path(Path.Combine(cwd, "..", "TestProject.csproj")), _output.ToString());
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
            var secretManager = new Program(_console, dir);

            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    _output.ToString());
            }

            _output.Clear();
            var args = fromCurrentDirectory
                ? new string[] { "list" }
                : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    _output.ToString());
            }

            // Remove secrets.
            _output.Clear();
            foreach (var secret in secrets)
            {
                var parameters = fromCurrentDirectory ?
                    new string[] { "remove", secret.Key } :
                    new string[] { "remove", secret.Key, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            // Verify secrets are removed.
            _output.Clear();
            args = fromCurrentDirectory
                ? new string[] { "list" }
                : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Contains(Resources.Error_No_Secrets_Found, _output.ToString());
        }

        [Fact]
        public void SetSecret_Update_Existing_Secret()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();

            secretManager.RunInternal("set", "secret1", "value1", "-p", projectPath);
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", _output.ToString());
            secretManager.RunInternal("set", "secret1", "value2", "-p", projectPath);
            Assert.Contains("Successfully saved secret1 = value2 to the secret store.", _output.ToString());

            _output.Clear();

            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Contains("secret1 = value2", _output.ToString());
        }

        [Fact]
        public void SetSecret_With_Verbose_Flag()
        {
            string secretId;
            var projectPath = _fixture.GetTempSecretProject(out secretId);
            var secretManager = CreateProgram();

            secretManager.RunInternal("-v", "set", "secret1", "value1", "-p", projectPath);
            Assert.Contains(string.Format("Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _output.ToString());
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _output.ToString());
            Assert.Contains("Successfully saved secret1 = value1 to the secret store.", _output.ToString());
            _output.Clear();

            secretManager.RunInternal("-v", "list", "-p", projectPath);

            Assert.Contains(string.Format("Project file path {0}.", Path.Combine(projectPath, "TestProject.csproj")), _output.ToString());
            Assert.Contains(string.Format("Secrets file path {0}.", PathHelper.GetSecretsPathFromSecretsId(secretId)), _output.ToString());
            Assert.Contains("secret1 = value1", _output.ToString());
        }

        [Fact]
        public void Remove_Non_Existing_Secret()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();
            secretManager.RunInternal("remove", "secret1", "-p", projectPath);
            Assert.Contains("Cannot find 'secret1' in the secret store.", _output.ToString());
        }

        [Fact]
        public void Remove_Is_Case_Insensitive()
        {
            var projectPath = _fixture.GetTempSecretProject();
            var secretManager = CreateProgram();
            secretManager.RunInternal("set", "SeCreT1", "value", "-p", projectPath);
            secretManager.RunInternal("list", "-p", projectPath);
            Assert.Contains("SeCreT1 = value", _output.ToString());
            secretManager.RunInternal("remove", "secret1", "-p", projectPath);

            _output.Clear();
            secretManager.RunInternal("list", "-p", projectPath);

            Assert.Contains(Resources.Error_No_Secrets_Found, _output.ToString());
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
            Assert.Contains("AzureAd:ClientSecret = abcdéƒ©˙î", _output.ToString());
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
            var stdout = _output.ToString();
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

            Assert.Contains("AzureAd:ClientSecret = ¡™£¢∞", _output.ToString());
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
            Assert.Contains(Resources.Error_No_Secrets_Found, _output.ToString());
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
                    new string[] { "set", secret.Key, secret.Value } :
                    new string[] { "set", secret.Key, secret.Value, "-p", projectPath };
                secretManager.RunInternal(parameters);
            }

            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("Successfully saved {0} = {1} to the secret store.", keyValue.Key, keyValue.Value),
                    _output.ToString());
            }

            // Verify secrets are persisted.
            _output.Clear();
            var args = fromCurrentDirectory ?
                new string[] { "list" } :
                new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            foreach (var keyValue in secrets)
            {
                Assert.Contains(
                    string.Format("{0} = {1}", keyValue.Key, keyValue.Value),
                    _output.ToString());
            }

            // Clear secrets.
            _output.Clear();
            args = fromCurrentDirectory ? new string[] { "clear" } : new string[] { "clear", "-p", projectPath };
            secretManager.RunInternal(args);

            args = fromCurrentDirectory ? new string[] { "list" } : new string[] { "list", "-p", projectPath };
            secretManager.RunInternal(args);
            Assert.Contains(Resources.Error_No_Secrets_Found, _output.ToString());
        }

        [Fact]
        public void Init_When_Project_Has_No_Secrets_Id()
        {
            var projectPath = _fixture.CreateProject(null);
            var project = Path.Combine(projectPath, "TestProject.csproj");
            var secretManager = new Program(_console, projectPath);

            secretManager.RunInternal("init", "-p", project);

            Assert.DoesNotContain(Resources.FormatError_ProjectMissingId(project), _output.ToString());
            Assert.DoesNotContain("--help", _output.ToString());
        }
    }
}
