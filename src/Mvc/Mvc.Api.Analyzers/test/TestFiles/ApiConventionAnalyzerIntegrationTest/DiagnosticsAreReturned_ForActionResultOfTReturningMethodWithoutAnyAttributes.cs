using System;
using Microsoft.AspNetCore.Mvc;

[assembly: ApiConventionType(typeof(DefaultApiConventions))]

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [ApiController]
    public class DiagnosticsAreReturned_ForActionResultOfTReturningMethodWithoutAnyAttributes : ControllerBase
    {
        public ActionResult<string> Method(Guid? id)
        {
            if (id == null)
            {
                /*MM*/return NotFound();
            }

            return "Hello world";
        }
    }
}
