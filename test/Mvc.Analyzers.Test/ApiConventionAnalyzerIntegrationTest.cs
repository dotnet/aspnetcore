// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ApiConventionAnalyzerIntegrationTest
    {
        private MvcDiagnosticAnalyzerRunner Executor { get; } = new ApiCoventionWith1006DiagnosticEnabledRunner();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForNonApiController()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForRazorPageModels()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForApiController_WithAllDocumentedStatusCodes()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForApiController_IfStatusCodesCannotBeInferred()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForReturnStatementsInLambdas()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task NoDiagnosticsAreReturned_ForReturnStatementsInLocalFunctions()
            => RunNoDiagnosticsAreReturned();

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 404);

        [Fact]
        public Task DiagnosticsAreReturned_IfAsyncMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 404);

        [Fact]
        public Task DiagnosticsAreReturned_IfAsyncMethodReturningValueTaskWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 200);

        [Fact]
        public Task DiagnosticsAreReturned_ForActionResultOfTReturningMethodWithoutAnyAttributes()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 404);

        [Fact]
        public Task DiagnosticsAreReturned_ForActionResultOfTReturningMethodWithoutSomeAttributes()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 422);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithConvention_ReturnsUndocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode, 400);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentation()
            => RunTest(DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentation()
            => RunTest(DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedType()
            => RunTest(DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_DoesNotReturnDocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode, 400);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithConvention_DoesNotReturnDocumentedStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode, 404);

        [Fact]
        public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_DoesNotDocumentSuccessStatusCode()
            => RunTest(DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode, 200);

        private async Task RunNoDiagnosticsAreReturned([CallerMemberName] string testMethod = "")
        {
            // Arrange
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Executor.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Empty(result);
        }

        private Task RunTest(DiagnosticDescriptor descriptor, [CallerMemberName] string testMethod = "")
            => RunTest(descriptor, Array.Empty<object>(), testMethod);

        private Task RunTest(DiagnosticDescriptor descriptor, int statusCode, [CallerMemberName] string testMethod = "")
            => RunTest(descriptor, new[] { statusCode.ToString() }, testMethod);

        private async Task RunTest(DiagnosticDescriptor descriptor, object[] args, [CallerMemberName] string testMethod = "")
        {
            // Arrange
            var testSource = MvcTestSource.Read(GetType().Name, testMethod);
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Executor.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {
                    Assert.Equal(descriptor.Id, diagnostic.Id);
                    Assert.Same(descriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                    Assert.Equal(string.Format(descriptor.MessageFormat.ToString(), args), diagnostic.GetMessage());
                });
        }
        private class ApiCoventionWith1006DiagnosticEnabledRunner : MvcDiagnosticAnalyzerRunner
        {
            public ApiCoventionWith1006DiagnosticEnabledRunner() : base(new ApiConventionAnalyzer())
            {
            }

            protected override CompilationOptions ConfigureCompilationOptions(CompilationOptions options)
            {
                var compilationOptions = base.ConfigureCompilationOptions(options);

                // 10006 is disabled by default. Explicitly enable it so we can correctly validate no diagnostics
                // are returned scenarios.
                var specificDiagnosticOptions = compilationOptions.SpecificDiagnosticOptions.Add(
                    DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode.Id,
                    ReportDiagnostic.Info);

                return compilationOptions.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
            }
        }
    }
}
