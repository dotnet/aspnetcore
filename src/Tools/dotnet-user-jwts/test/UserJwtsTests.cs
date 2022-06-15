// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Tools.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer.Tools;
using Xunit;
using Xunit.Abstractions;
using System.Text.RegularExpressions;

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
}
