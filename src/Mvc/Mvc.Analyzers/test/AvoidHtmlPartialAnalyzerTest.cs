// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class AvoidHtmlPartialAnalyzerTest
    {
        private static DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC1000_HtmlHelperPartialShouldBeAvoided;

        private MvcDiagnosticAnalyzerRunner Executor { get; } = new MvcDiagnosticAnalyzerRunner(new AvoidHtmlPartialAnalyzer());

        [Fact]
        public Task NoDiagnosticsAreReturned_FoEmptyScenarios()
            => VerifyNoDiagnosticsAreReturned(source: string.Empty);

        [Fact]
        public Task NoDiagnosticsAreReturned_ForNonUseOfHtmlPartial()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource().Source);

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfHtmlPartialAsync()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource().Source);

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfHtmlPartial()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfHtmlPartial_WithAdditionalParameters()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfHtmlPartial_InSections()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task NoDiagnosticsAreReturned_ForUseOfRenderPartialAsync()
            => VerifyNoDiagnosticsAreReturned(ReadTestSource().Source);

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfRenderPartial()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfRenderPartial_WithAdditionalParameters()
            => VerifyDefault(ReadTestSource());

        [Fact]
        public Task DiagnosticsAreReturned_ForUseOfRenderPartial_InSections()
            => VerifyDefault(ReadTestSource());

        private async Task VerifyNoDiagnosticsAreReturned(string source)
        {
            // Act
            var result = await Executor.GetDiagnosticsAsync(source);

            // Assert
            Assert.Empty(result);
        }

        private async Task VerifyDefault(TestSource testSource)
        {
            // Arrange
            var expectedLocation = testSource.DefaultMarkerLocation;

            // Act
            var result = await Executor.GetDiagnosticsAsync(testSource.Source);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        private static TestSource ReadTestSource([CallerMemberName] string testMethod = "") =>
            MvcTestSource.Read(nameof(AvoidHtmlPartialAnalyzerTest), testMethod);
    }
}
