// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ActionsMustNotBeAsyncVoidFacts : AnalyzerTestBase
    {
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
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7003",
                Message = "Controller actions must not have async void signature.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 7, 18) }
            };
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
            Assert.DiagnosticsEqual(new[] { expectedDiagnostic }, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_WhenActionMethodIsExpressionBodied()
        {
            // Arrange
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7003",
                Message = "Controller actions must not have async void signature.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 7, 18) }
            };
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
            Assert.DiagnosticsEqual(new[] { expectedDiagnostic }, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CodeFix_ProducesFullyQualifiedNamespaces()
        {
            // Arrange
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7003",
                Message = "Controller actions must not have async void signature.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 6, 18) }
            };
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
            Assert.DiagnosticsEqual(new[] { expectedDiagnostic }, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }
    }
}
