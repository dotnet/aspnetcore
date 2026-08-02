// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.InspectReturnExpressionTestsForSwitchExpression
{
    public class TestController : ControllerBase
    {
        public object InspectReturnExpression_ReturnsDefaultResponseMetadata_IfReturnedTypeIsNotActionResult()
        {
            return true switch
            {
                _ => new TestModel()
            };
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromDefaultStatusCodeAttributeOnActionResult()
        {
            return true switch
            {
                _ => Unauthorized()
            };
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromStatusCodePropertyAssignment()
        {
            return true switch
            {
                _ => new ObjectResult(new object()) { StatusCode = 201 }
            };
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromConstructorAssignment()
        {
            return true switch
            {
                _ => new StatusCodeResult(204)
            };
        }

        public IActionResult InspectReturnExpression_ReturnsStatusCodeFromHelperMethod()
        {
            return true switch
            {
                _ => StatusCode(302)
            };
        }

        public IActionResult InspectReturnExpression_UsesExplicitlySpecifiedStatusCode_ForActionResultWithDefaultStatusCode()
        {
            return true switch
            {
                _ => new BadRequestObjectResult(new object())
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity,
                }
            };
        }

        public IActionResult InspectReturnExpression_ReadsStatusCodeConstant()
        {
            return true switch
            {
                _ => StatusCode(StatusCodes.Status423Locked)
            };
        }

        public IActionResult InspectReturnExpression_DoesNotReadLocalFieldWithConstantValue()
        {
            var statusCode = StatusCodes.Status429TooManyRequests;

            return true switch
            {
                _ => StatusCode(statusCode)
            };
        }

        public IActionResult InspectReturnExpression_FallsBackToDefaultStatusCode_WhenAppliedStatusCodeCannotBeRead()
        {
            var statusCode = StatusCodes.Status422UnprocessableEntity;

            return true switch
            {
                _ => new BadRequestObjectResult(new object()) { StatusCode = statusCode }
            };
        }

        public IActionResult InspectReturnExpression_SetsReturnType_WhenLiteralTypeIsSpecifiedInConstructor()
        {
            return true switch
            {
                _ => new BadRequestObjectResult(new TestModel())
            };
        }

        public IActionResult InspectReturnExpression_SetsReturnType_WhenLocalValueIsSpecifiedInConstructor()
        {
            var local = new TestModel();

            return true switch
            {
                _ => new BadRequestObjectResult(local)
            };
        }

        public IActionResult InspectReturnExpression_ReturnsNullReturnType_IfValueIsNotSpecified()
        {
            return true switch
            {
                _ => NotFound()
            };
        }

        public ActionResult<TestModel> InspectReturnExpression_SetsReturnType_WhenValueIsReturned()
        {
            var local = new TestModel();

            return true switch
            {
                _ => local
            };
        }
    }

    public class TestModel { }
}
