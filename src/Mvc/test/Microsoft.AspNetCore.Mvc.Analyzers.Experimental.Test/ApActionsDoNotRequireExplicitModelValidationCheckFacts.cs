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
    public class ApiActionsDoNotRequireExplicitModelValidationCheckFacts : AnalyzerTestBase
    {
        protected override DiagnosticAnalyzer DiagnosticAnalyzer { get; }
            = new ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer();

        protected override CodeFixProvider CodeFixProvider { get; }
            = new ApiActionsDoNotRequireExplicitModelValidationCheckCodeFixProvider();

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
        public async Task NoDiagnosticsAreReturned_WhenActionDoesNotHaveModelStateCheck()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : Controller
{
    public IActionResult GetPetId()
    {
        return Ok(new object());
    }
}";
            var project = CreateProject(test);

            // Act
            var result = await GetDiagnosticAsync(project);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task NoDiagnosticsAreReturned_WhenAActionsUseExpressionBodies()
        {
            // Arrange
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : Controller
{
    public IActionResult GetPetId() => ModelState.IsVald ? OK() : BadResult();
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
public class PetController : ControllerBase
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
        public Task DiagnosticsAndCodeFixes_WhenActionHasModelStateIsValidCheck()
        {
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        return Ok();
    }
}";

            // Act & Assert
            return VerifyAsync(test);
        }


        [Fact]
        public Task DiagnosticsAndCodeFixes_WhenActionHasModelStateIsValidCheck_UsingComparisonToFalse()
        {
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (ModelState.IsValid == false)
        {
            return BadRequest();
        }

        return Ok();
    }
}";

            // Act & Assert
            return VerifyAsync(test);
        }

        [Fact]
        public Task DiagnosticsAndCodeFixes_WhenActionHasModelStateIsValidCheck_WithoutBraces()
        {
            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (!ModelState.IsValid)
            return BadRequest();

        return Ok();
    }
}";
            return VerifyAsync(test);
        }

        private async Task VerifyAsync(string test)
        {
            // Arrange
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7001",
                Message = "Actions on types annotated with ApiControllerAttribute do not require explicit ModelState validity check.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 9, 9) }
            };
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        return Ok();
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
        public async Task DiagnosticsAndCodeFixes_WhenModelStateIsInElseIf()
        {
            // Arrange
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7001",
                Message = "Actions on types annotated with ApiControllerAttribute do not require explicit ModelState validity check.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 13, 9) }
            };

            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (User == null)
        {
            return Unauthorized();
        }
        else if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok();
    }
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (User == null)
        {
            return Unauthorized();
        }

        return Ok();
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
        public async Task DiagnosticsAndCodeFixes_WhenModelStateIsInNestedBlock()
        {
            // Arrange
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = "MVC7001",
                Message = "Actions on types annotated with ApiControllerAttribute do not require explicit ModelState validity check.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test.cs", 15, 13) }
            };

            var test =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (User == null)
        {
            return Unauthorized();
        }
        else
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Debug.Assert(ModelState.Count == 0);
        }

        return Ok();
    }
}";
            var expectedFix =
@"
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class PetController : ControllerBase
{
    public IActionResult GetPetId()
    {
        if (User == null)
        {
            return Unauthorized();
        }
        else
        {
            Debug.Assert(ModelState.Count == 0);
        }

        return Ok();
    }
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