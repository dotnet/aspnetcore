// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers._INPUT_
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class CodeFixWithConventionAddsMissingStatusCodes : ControllerBase
    {
        public ActionResult<string> GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            return Accepted("Result");
        }
    }
}
