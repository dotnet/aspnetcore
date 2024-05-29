// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.Dependencies.AddPackageFixer>;

namespace Microsoft.AspNetCore.Analyzers.Dependencies;

/// <remarks>
/// These tests don't assert the fix is applied, since it takes a dependency on the internal
/// VS-specific `PackageInstallerService`. However, the fixer is invoked in these codepaths
/// so we can validate that the symbol resolution and checks function correctly.
/// </remarks>
public class AddPackagesTest
{
    [Fact]
    public async Task CanFixMissingExtensionMethodForDI()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.{|CS1061:AddOpenApi|}();
";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }

    [Fact]
    public async Task CanFixMissingExtensionMethodForBuilder()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var app = WebApplication.Create();

app.{|CS1061:MapOpenApi|}();
";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, source);
    }
}
