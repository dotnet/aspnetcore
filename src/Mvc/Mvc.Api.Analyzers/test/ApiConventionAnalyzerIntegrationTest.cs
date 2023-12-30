// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Analyzer.Testing;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

public class ApiConventionAnalyzerIntegrationTest
{
    private MvcDiagnosticAnalyzerRunner Executor { get; } = new ApiConventionWith1006DiagnosticEnabledRunner();

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
    public Task NoDiagnosticsAreReturned_ForOkResultReturningAction()
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
    public Task NoDiagnosticsAreReturned_ForApiController_WhenMethodNeverReturns()
        => RunNoDiagnosticsAreReturned();

    [Fact]
    public async Task DiagnosticsAreReturned_ForIncompleteActionResults()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;

namespace Test
{
    [ApiController]
    [Route(""[controller]/[action]"")
    public class TestController : ControllerBase
    {
        public IActionResult Get(int id)
        {
            if (id == 0)
            {
                return /*MM*/NotFound();
            }

            return;
        }
    }
}";
        var testSource = TestSource.Read(source);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await Executor.GetDiagnosticsAsync(testSource.Source);

        // Assert
        var diagnostic = Assert.Single(result, d => d.Id == ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id);
        AnalyzerAssert.DiagnosticLocation(expectedLocation, diagnostic.Location);
    }

    [Fact]
    public async Task NoDiagnosticsAreReturned_WhenActionDoesNotCompile()
    {
        // Arrange
        var source = @"
namespace Test
{
    [ApiController]
    [Route(""[controller]/[action]"")
    public class TestController : ControllerBase
    {
        public IActionResult Get(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Ok();
        }
    }
}";

        // Act
        var result = await Executor.GetDiagnosticsAsync(source);

        // Assert
        Assert.DoesNotContain(result, d => d.Id == ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id);
    }

    [Fact]
    public async Task DiagnosticsAreReturned_ForNotFoundNullActionResults()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;

namespace Test
{
    [ApiController]
    [Route(""[controller]"")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Test()
        {
            return NotFound(null);
        }
    }
}";
        var testSource = TestSource.Read(source);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await Executor.GetDiagnosticsAsync(testSource.Source);

        // Assert
        Assert.Contains(result, d => d.Id == ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id);
    }

    [Fact]
    public async Task DiagnosticsAreReturned_ForActionsReturnedFromSwitchExpression()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;

namespace Test
{
    [ApiController]
    public class Foo : ControllerBase
    {
        public IActionResult Get(bool b)
        {
            return b switch
            {
                true => Ok(),
                false => BadRequest()
            };
        }
    }
}";
        var testSource = TestSource.Read(source);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await Executor.GetDiagnosticsAsync(testSource.Source);

        // Assert
        Assert.Contains(result, d => d.Id == ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id);
    }

        [Fact]
    public async Task DiagnosticsAreReturned_ForActionsReturnedFromSwitchStatement()
    {
        // Arrange
        var source = @"
using Microsoft.AspNetCore.Mvc;

namespace Test
{
    [ApiController]
    public class Foo : ControllerBase
    {
        public IActionResult Get(bool b)
        {
            switch (b)
            {
                case true:
                    return Ok();
                case false:
                    return BadRequest();
            }
        }
    }
}";
        var testSource = TestSource.Read(source);
        var expectedLocation = testSource.DefaultMarkerLocation;

        // Act
        var result = await Executor.GetDiagnosticsAsync(testSource.Source);

        // Assert
        Assert.Contains(result, d => d.Id == ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode.Id);
    }

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 404);

    [Fact]
    public Task DiagnosticsAreReturned_IfAsyncMethodWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 404);

    [Fact]
    public Task DiagnosticsAreReturned_IfAsyncMethodReturningValueTaskWithProducesResponseTypeAttribute_ReturnsUndocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 200);

    [Fact]
    public Task DiagnosticsAreReturned_ForActionResultOfTReturningMethodWithoutAnyAttributes()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 404);

    [Fact]
    public Task DiagnosticsAreReturned_ForActionResultOfTReturningMethodWithoutSomeAttributes()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 422);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithConvention_ReturnsUndocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 400);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithApiConventionMethod_ReturnsUndocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode, 202);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithAttributeReturnsValue_WithoutDocumentation()
        => RunTest(ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithAttributeAsynchronouslyReturnsValue_WithoutDocumentation()
        => RunTest(ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithAttribute_ReturnsDerivedType()
        => RunTest(ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_DoesNotReturnDocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode, 400);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithConvention_DoesNotReturnDocumentedStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode, 404);

    [Fact]
    public Task DiagnosticsAreReturned_IfMethodWithProducesResponseTypeAttribute_DoesNotDocumentSuccessStatusCode()
        => RunTest(ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode, 200);

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
        => RunTest(descriptor, new[] { statusCode.ToString(CultureInfo.InvariantCulture) }, testMethod);

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
                Assert.Equal(string.Format(CultureInfo.InvariantCulture, descriptor.MessageFormat.ToString(CultureInfo.InvariantCulture), args), diagnostic.GetMessage(CultureInfo.InvariantCulture));
            });
    }
    private class ApiConventionWith1006DiagnosticEnabledRunner : MvcDiagnosticAnalyzerRunner
    {
        public ApiConventionWith1006DiagnosticEnabledRunner() : base(new ApiConventionAnalyzer())
        {
        }

        protected override CompilationOptions ConfigureCompilationOptions(CompilationOptions options)
        {
            var compilationOptions = base.ConfigureCompilationOptions(options);

            // 10006 is disabled by default. Explicitly enable it so we can correctly validate no diagnostics
            // are returned scenarios.
            var specificDiagnosticOptions = compilationOptions.SpecificDiagnosticOptions.Add(
                ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode.Id,
                ReportDiagnostic.Info);

            return compilationOptions.WithSpecificDiagnosticOptions(specificDiagnosticOptions);
        }
    }
}
