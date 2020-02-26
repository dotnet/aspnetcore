// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

[assembly: Microsoft.AspNetCore.Mvc.ApiConventionType(typeof(Microsoft.AspNetCore.Mvc.DefaultApiConventions))]

namespace TestApp._OUTPUT_
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]/[action]")]
    public class BaseController : ControllerBase
    {

    }
}

namespace TestApp._OUTPUT_
{
    public class CodeFixAddsFullyQualifiedProducesResponseType : BaseController
    {
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(StatusCodes.Status202Accepted)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(StatusCodes.Status404NotFound)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseType]
        public object GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            if (id == 1)
            {
                return BadRequest();
            }

            return Accepted(new object());
        }
    }
}
