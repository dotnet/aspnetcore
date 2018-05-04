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
    public class ApiActionsShouldUseActionResultOfTFacts : AnalyzerTestBase
    {
        private static DiagnosticDescriptor DiagnosticDescriptor = DiagnosticDescriptors.MVC7002_ApiActionsShouldReturnActionResultOf;

        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; }
            = new ApiActionsShouldUseActionResultOfTAnalyzer();

        protected override CodeFixProvider CodeFixProvider { get; }
            = new ApiActionsShouldUseActionResultOfTCodeFixProvider();

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

public class HomeController:  ControllerBase
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
        public async Task NoDiagnosticsAreReturned_ForNonActions()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController:  ControllerBaseBase
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
        public async Task NoDiagnosticsAreReturned_WhenActionAreExpressionBodiedMembers()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController:  ControllerBase
{
    public IActionResult GetPetId() => ModelState.IsValid ? OK(new object()) : BadResult();
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("Pet")]
        [InlineData("List<Pet>")]
        [InlineData("System.Threading.Task<Pet>")]
        public async Task NoDiagnosticsAreReturned_WhenTypeReturnsNonObjectResult(string returnType)
        {
            // Arrange
            var test =
$@"
using Microsoft.AspNetCore.Mvc;

public class Pet {{ }}

[ApiController]
public class PetController:  ControllerBase
{{
    public {returnType} GetPetId() => null;
}}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async     Task NoDiagnosticsAreReturned_WhenTypeReturnsActionResultOfT()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

public class Pet { }

[ApiController]
public class PetController:  ControllerBase
{
    public ActionResult<Pet> GetPetId() => null;
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_WhenActionsReturnIActionResult()
        {
            // Arrange
            var expectedLocation = new DiagnosticLocation("Test.cs", 9, 12);
            var test =
@"
using Microsoft.AspNetCore.Mvc;

public class Pet {}

[ApiController]
public class PetController:  ControllerBase
{
    public IActionResult GetPet()
    {
        return Ok(new Pet());
    }
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

public class Pet {}

[ApiController]
public class PetController:  ControllerBase
{
    public ActionResult<Pet> GetPet()
    {
        return Ok(new Pet());
    }
}";
            var project = CreateProject(test);

            // Act
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(expectedLocation, actualDiagnostics);

            var actualFix = await ApplyCodeFixAsync(project, actualDiagnostics);
            Assert.Equal(expectedFix, actualFix, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task DiagnosticsAreReturned_WhenActionReturnsAsyncIActionResult()
        {
            // Arrange
            var expectedLocation = new DiagnosticLocation("Test.cs", 8, 18);

            var test =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
public class PetController:  ControllerBase
{
    public async Task<IActionResult> GetPet()
    {
        await Task.Delay(0);
        return Ok(new Pet());
    }
}
public class Pet {}";

            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
public class PetController:  ControllerBase
{
    public async Task<ActionResult<Pet>> GetPet()
    {
        await Task.Delay(0);
        return Ok(new Pet());
    }
}
public class Pet {}";
            var project = CreateProject(test);

            // Act & Assert
            var actualDiagnostics = await GetDiagnosticAsync(project);
            AssertDiagnostic(expectedLocation, actualDiagnostics);

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
