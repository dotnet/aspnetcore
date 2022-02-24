// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzerIntegrationTest
{
    private MvcDiagnosticAnalyzerRunner AnalyzerRunner { get; } = new MvcDiagnosticAnalyzerRunner(new ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer());

    [Fact]
    public Task NoDiagnosticsAreReturned_ForNonApiController()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task NoDiagnosticsAreReturned_ForRazorPageModels()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task NoDiagnosticsAreReturned_ForApiActionsWithoutModelStateChecks()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task NoDiagnosticsAreReturned_ForApiActionsReturning400FromNonModelStateIsValidBlocks()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task NoDiagnosticsAreReturned_ForApiActionsReturningNot400FromNonModelStateIsValidBlock()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task NoDiagnosticsAreReturned_ForApiActionsCheckingAdditionalConditions()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public Task DiagnosticsAreReturned_ForApiActionsWithModelStateChecks()
        => RunTest();

    [Fact]
    public Task DiagnosticsAreReturned_ForApiActionsWithModelStateChecksUsingEquality()
        => RunTest();

    [Fact]
    public Task DiagnosticsAreReturned_ForApiActionsWithModelStateChecksWithoutBracing()
        => RunTest();

    private async Task RunNoDiagnosticsAreReturned([CallerMemberName] string testMethod = "")
    {
        // Arrange
        var testSource = MvcTestSource.Read(GetType().Name, testMethod);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await AnalyzerRunner.GetDiagnosticsAsync(testSource.Source);

        // Assert
        Assert.Empty(result);
    }

    private async Task RunTest([CallerMemberName] string testMethod = "")
    {
        // Arrange
        var descriptor = ApiDiagnosticDescriptors.API1003_ApiActionsDoNotRequireExplicitModelValidationCheck;
        var testSource = MvcTestSource.Read(GetType().Name, testMethod);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await AnalyzerRunner.GetDiagnosticsAsync(testSource.Source);

        // Assert
        Assert.Collection(
            result,
            diagnostic =>
            {
                Assert.Equal(descriptor.Id, diagnostic.Id);
                Assert.Same(descriptor, diagnostic.Descriptor);
                AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
            });
    }
}
