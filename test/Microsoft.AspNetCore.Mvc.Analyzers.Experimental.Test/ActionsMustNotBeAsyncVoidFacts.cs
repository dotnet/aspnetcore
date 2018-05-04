// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ActionsMustNotBeAsyncVoidFacts : AnalyzerTestBase
    {
        private static DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC7003_ActionsMustNotBeAsyncVoid;

        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; }
            = new ActionsMustNotBeAsyncVoidAnalyzer();

        protected override CodeFixProvider CodeFixProvider { get; }
            = new ActionsMustNotBeAsyncVoidFixProvider();

        [Fact]
        public async Task NoDiagnosticsAreReturned_FoEmptyScenarios()
        {
            // Arrange
            var test = @"";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_WhenMethodIsNotAControllerAction()
        {
            // Arrange
            var test =
@"
using System.Threading.Tasks;

public class UserViewModel
{
    public async void Index() => await Task.Delay(10);
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_WhenMethodIsAControllerAction()
        {
            // Arrange
            var location = new DiagnosticLocation("Test.cs", 7, 18);
            var test =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class HomeController : Controller
{
    public async void Index()
    {
        await Response.Body.FlushAsync();
    }
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class HomeController : Controller
{
    public async Task Index()
    {
        await Response.Body.FlushAsync();
    }
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(location, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_WhenActionMethodIsExpressionBodied()
        {
            // Arrange
            var location = new DiagnosticLocation("Test.cs", 7, 18);
            var test =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class HomeController : Controller
{
    public async void Index() => await Response.Body.FlushAsync();
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class HomeController : Controller
{
    public async Task Index() => await Response.Body.FlushAsync();
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(location, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CodeFix_ProducesFullyQualifiedNamespaces()
        {
            // Arrange
            var location = new DiagnosticLocation("Test.cs", 6, 18);
            var test =
@"
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public async void Index() => await Response.Body.FlushAsync();
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public async System.Threading.Tasks.Task Index() => await Response.Body.FlushAsync();
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(location, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        private void AssertDiagnostic(DiagnosticLocation expectedLocation, Diagnostic[] actualDiagnostics)
        {
            // Assert
            Assert.Collection(
                actualDiagnostics,
                diagnostic =>
                {
                    Assert.Equal(DiagnosticDescriptor.Id, diagnostic.Id);
                    Assert.Same(DiagnosticDescriptor, diagnostic.Descriptor);
                    AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
                });
        }
    }
}
