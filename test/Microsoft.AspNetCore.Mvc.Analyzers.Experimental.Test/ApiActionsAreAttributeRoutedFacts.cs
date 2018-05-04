// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.AspNetCore.Mvc.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class ApiActionsAreAttributeRoutedFacts : AnalyzerTestBase
    {
        private static DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC7000_ApiActionsMustBeAttributeRouted;

        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; }
            = new ApiActionsAreAttributeRoutedAnalyzer();

        protected override CodeFixProvider CodeFixProvider { get; }
            = new ApiActionsAreAttributeRoutedFixProvider();

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
        public async Task NoDiagnosticsAreReturned_WhenTypeIsNotApiController()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_WhenApiControllerActionHasAttribute()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : Controller
{
    [HttpGet]
    public int GetPetId() => 0;
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_ForConstructors()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : Controller
{
    public PetController(){ }
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_ForNonActions()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : Controller
{
    private int GetPetIdPrivate() => 0;
    protected int GetPetIdProtected() => 0;
    public static IActionResult FindPetByStatus(int status) => null;
    [NonAction]
    public object Reset(int state) => null;
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiagnosticsAndCodeFixes_WhenApiControllerActionDoesNotHaveAttribute()
        {
            // Arrange
            var expectedLocation = new DiagnosticLocation("Test.cs", 8, 16);
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route]
public class PetController : Controller
{
    public int GetPetId() => 0;
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route]
public class PetController : Controller
{
    [HttpGet]
    public int GetPetId() => 0;
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(expectedLocation, actualDiagnostics);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CodeFixes_ApplyFullyQualifiedNames()
        {
            // Arrange
            var test =
@"
[Microsoft.AspNetCore.Mvc.ApiController]
[Microsoft.AspNetCore.Mvc.Route]
public class PetController
{
    public object GetPet() => null;
}";
            var expectedFix =
@"
[Microsoft.AspNetCore.Mvc.ApiController]
[Microsoft.AspNetCore.Mvc.Route]
public class PetController
{
    [Microsoft.AspNetCore.Mvc.HttpGet]
    public object GetPet() => null;
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [InlineData("id")]
        [InlineData("petId")]
        public async Task CodeFixes_WithIdParameter(string idParameter)
        {
            // Arrange
            var test =
$@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{{
    public IActionResult Post(string notid, int {idParameter}) => null;
}}";
            var expectedFix =
$@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{{
    [HttpPost(""{{{idParameter}}}"")]
    public IActionResult Post(string notid, int {idParameter}) => null;
}}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CodeFixes_WithRouteParameter()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{
    public IActionResult DeletePetByStatus([FromRoute] Status status, [FromRoute] Category category) => null;
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{
    [HttpDelete(""{status}/{category}"")]
    public IActionResult DeletePetByStatus([FromRoute] Status status, [FromRoute] Category category) => null;
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CodeFixes_WhenAttributeCannotBeInferred()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{
    public IActionResult ModifyPet() => null;
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;
[ApiController]
[Route]
public class PetController
{
    [HttpPut]
    public IActionResult ModifyPet() => null;
}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            // There isn't a good way to test all fixes simultaneously. We'll pick the last one to verify when we
            // expect to have 4 fixes.
            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics, codeFixIndex: 3);
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