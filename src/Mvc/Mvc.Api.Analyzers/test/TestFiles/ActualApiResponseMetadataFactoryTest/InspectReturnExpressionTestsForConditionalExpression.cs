// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers.TestFiles.InspectReturnExpressionTestsForConditionalExpression
{
    public class TestController : ControllerBase
    {
        public IActionResult InspectReturnExpression_ReadsBothBranchesOfConditional(int id)
        {
            return id == 0 ? NotFound() : Ok(new TestModel());
        }

        public IActionResult InspectReturnExpression_ReadsNestedConditional(int id)
        {
            return id == 0
                ? NotFound()
                : id == 1
                    ? Unauthorized()
                    : Ok(new TestModel());
        }
    }

    public class TestModel { }
}
