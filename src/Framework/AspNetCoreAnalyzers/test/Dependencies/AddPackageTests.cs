// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.AddPackage;
using Moq;
using VerifyCS = Microsoft.AspNetCore.Analyzers.Verifiers.CSharpCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.WebApplicationBuilderAnalyzer,
    Microsoft.AspNetCore.Analyzers.Dependencies.AddPackagesTest.MockAddPackageFixer>;

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
        MockAddPackageFixer.Invoked = false;
        await VerifyCS.VerifyCodeFixAsync(source, source);
        Assert.True(MockAddPackageFixer.Invoked);
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
        MockAddPackageFixer.Invoked = false;
        await VerifyCS.VerifyCodeFixAsync(source, source);
        Assert.True(MockAddPackageFixer.Invoked);
    }

    public class MockAddPackageFixer : AddPackageFixer
    {
        /// <remarks>
        /// This static property allows us to verify that the fixer was
        /// able to successfully resolve the symbol and call into the
        /// package install APIs. This is a workaround for the fact that
        /// the package install APIs are not readily mockable. Note: this
        /// is not intended for use across test classes.
        /// </remarks>
        internal static bool Invoked { get; set; }

        internal override Task<CodeAction> TryCreateCodeActionAsync(
            Document document,
            int position,
            AspNetCoreInstallPackageData packageInstallData,
            CancellationToken cancellationToken)
        {
            Invoked = true;
            Assert.Equal("Microsoft.AspNetCore.OpenApi", packageInstallData.PackageName);
            return Task.FromResult<CodeAction>(null);
        }
    }
}
