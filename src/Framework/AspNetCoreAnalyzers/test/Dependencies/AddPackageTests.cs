// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.Dependencies.AddPackageFixer>;

namespace Microsoft.AspNetCore.Analyzers.Dependencies;
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
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
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
        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var app = WebApplication.Create(args);

app.MapOpenApi();
";

        // Assert
        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}
