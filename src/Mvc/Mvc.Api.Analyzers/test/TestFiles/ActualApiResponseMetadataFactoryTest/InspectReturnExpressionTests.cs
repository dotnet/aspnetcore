// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.InspectReturnExpressionTests
{
    public class TestController : ControllerBase
    {
        public object InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResult()
        {
            return new TestModel();
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult()
        {
            return Unauthorized();
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromStatusCodePropertyAssignment()
        {
            return new ObjectResult(new object()) { StatusCode = 201 };
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromConstructorAssignment()
        {
            return new StatusCodeResult(204);
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromHelperMethod()
        {
            return StatusCode(302);
        }

        public IActionResult InspectReturnExpression_UsesExplicitlySpecifiedStatusCode_ForActionResultWithDefaultStatusCode()
        {
            return new BadRequestObjectResult(new object())
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
            };
        }

        public IActionResult InspectReturnExpression_ReadsStatusCodeConstant()
        {
            return StatusCode(StatusCodes.Status423Locked);
        }

        public IActionResult InspectReturnExpression_DoesNotReadLocalFieldWithConstantValue()
        {
            var statusCode = StatusCodes.Status429TooManyRequests;
            return StatusCode(statusCode);
        }

        public IActionResult InspectReturnExpression_FallsBackToDefaultStatusCode_WhenAppliedStatusCodeCannotBeRead()
        {
            var statusCode = StatusCodes.Status422UnprocessableEntity;
            return new BadRequestObjectResult(new object()) { StatusCode = statusCode };
        }

        public IActionResult InspectReturnExpression_SetsReturnType_WhenLiteralTypeIsSpecifiedInConstructor()
        {
            return new BadRequestObjectResult(new TestModel());
        }

        public IActionResult InspectReturnExpression_SetsReturnType_WhenLocalValueIsSpecifiedInConstructor()
        {
            var local = new TestModel();
            return new BadRequestObjectResult(local);
        }

        public IActionResult InspectReturnExpression_ReturnsNullReturnType_IfValueIsNotSpecified()
        {
            return NotFound();
        }

        public ActionResult<TestModel> InspectReturnExpression_SetsReturnType_WhenValueIsReturned()
        {
            var local = new TestModel();
            return local;
        }
    }

    public class TestModel { }
}
