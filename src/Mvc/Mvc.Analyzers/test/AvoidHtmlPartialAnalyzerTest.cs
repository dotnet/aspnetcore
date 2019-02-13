// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class AvoidHtmlPartialAnalyzerTest : AnalyzerTestBase
    {
        private static DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC1000_HtmlHelperPartialShouldBeAvoided;

        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; } = new AvoidHtmlPartialAnalyzer();

        [Fact]
        public async Task NoDiagnosticsAreReturned_FoEmptyScenarios()
        {
            // Arrange
            var project = CreateProject(source: string.Empty);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_ForNonUseOfHtmlPartial()
        {
            // Arrange
            var project = CreateProjectFromFile();

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_ForUseOfHtmlPartialAsync()
        {
            // Arrange
            var project = CreateProjectFromFile();

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfHtmlPartial()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfHtmlPartial_WithAdditionalParameters()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfHtmlPartial_InSections()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_ForUseOfRenderPartialAsync()
        {
            // Arrange
            var project = CreateProjectFromFile();

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfRenderPartial()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfRenderPartial_WithAdditionalParameters()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }

        [Fact]
        public async Task DiagnosticsAreReturned_ForUseOfRenderPartial_InSections()
        {
            // Arrange
            var project = CreateProjectFromFile();
            var expectedLocation = DefaultMarkerLocation.Value;

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Collection(
                result,
                diagnostic =>
                {

                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    Assert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }
    }
}
