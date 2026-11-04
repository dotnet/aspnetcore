// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration.UserSecrets.Tests;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.SecretManager.Tools.Tests;

public class InitCommandTests : IClassFixture<UserSecretsTestFixture>
{
    private UserSecretsTestFixture _fixture;
    private ITestOutputHelper _output;
    private TestConsole _console;

    public InitCommandTests(UserSecretsTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;

        _console = new TestConsole(output);
    }

    private CommandContext MakeCommandContext() => new CommandContext(null, new TestReporter(_output), _console);

    [Fact]
    public void AddsSecretIdToProject()
    {
        var projectDir = _fixture.CreateProject(null);

        new InitCommand(null, null).Execute(MakeCommandContext(), projectDir);

        var idResolver = new ProjectIdResolver(MakeCommandContext().Reporter, projectDir);

        Assert.False(string.IsNullOrWhiteSpace(idResolver.Resolve(null, null)));
    }

    [Fact]
    public void AddsSpecificSecretIdToProject()
    {
        const string SecretId = "TestSecretId";

        var projectDir = _fixture.CreateProject(null);

        new InitCommand(SecretId, null).Execute(MakeCommandContext(), projectDir);

        var idResolver = new ProjectIdResolver(MakeCommandContext().Reporter, projectDir);

        Assert.Equal(SecretId, idResolver.Resolve(null, null));
    }

    [Fact]
    public void AddsEscapedSpecificSecretIdToProject()
    {
        const string SecretId = @"<lots of XML invalid values>&";

        var projectDir = _fixture.CreateProject(null);

        new InitCommand(SecretId, null).Execute(MakeCommandContext(), projectDir);

        var idResolver = new ProjectIdResolver(MakeCommandContext().Reporter, projectDir);

        Assert.Equal(SecretId, idResolver.Resolve(null, null));
    }

    [Fact]
    public void DoesNotGenerateIdForProjectWithSecretId()
    {
        const string SecretId = "AlreadyExists";

        var projectDir = _fixture.CreateProject(SecretId);

        new InitCommand(null, null).Execute(MakeCommandContext(), projectDir);

        var idResolver = new ProjectIdResolver(MakeCommandContext().Reporter, projectDir);

        Assert.Equal(SecretId, idResolver.Resolve(null, null));
    }

    [Fact]
    public void DoesNotAddXmlDeclarationToProject()
    {
        var projectDir = _fixture.CreateProject(null);
        var projectFile = Path.Combine(projectDir, "TestProject.csproj");

        new InitCommand(null, null).Execute(MakeCommandContext(), projectDir);

        var projectDocument = XDocument.Load(projectFile);
        Assert.Null(projectDocument.Declaration);
    }

    [Fact]
    public void DoesNotRemoveBlankLines()
    {
        var projectDir = _fixture.CreateProject(null);
        var projectFile = Path.Combine(projectDir, "TestProject.csproj");
        var projectDocumentWithoutSecret = XDocument.Load(projectFile, LoadOptions.PreserveWhitespace);
        var lineCountWithoutSecret = projectDocumentWithoutSecret.ToString().Split(Environment.NewLine).Length;

        new InitCommand(null, null).Execute(MakeCommandContext(), projectDir);

        var projectDocumentWithSecret = XDocument.Load(projectFile, LoadOptions.PreserveWhitespace);
        var lineCountWithSecret = projectDocumentWithSecret.ToString().Split(Environment.NewLine).Length;

        Assert.True(lineCountWithSecret == lineCountWithoutSecret + 1);
    }

    [Fact]
    public void OverridesIdForProjectWithSecretId()
    {
        const string SecretId = "AlreadyExists";
        const string NewId = "TestValue";

        var projectDir = _fixture.CreateProject(SecretId);

        new InitCommand(NewId, null).Execute(MakeCommandContext(), projectDir);

        var idResolver = new ProjectIdResolver(MakeCommandContext().Reporter, projectDir);

        Assert.Equal(NewId, idResolver.Resolve(null, null));
    }

    [Fact]
    public void FailsForInvalidId()
    {
        string secretId = $"invalid{Path.GetInvalidPathChars()[0]}secret-id";

        var projectDir = _fixture.CreateProject(null);

        Assert.Throws<ArgumentException>(() =>
        {
            new InitCommand(secretId, null).Execute(MakeCommandContext(), projectDir);
        });
    }
}
