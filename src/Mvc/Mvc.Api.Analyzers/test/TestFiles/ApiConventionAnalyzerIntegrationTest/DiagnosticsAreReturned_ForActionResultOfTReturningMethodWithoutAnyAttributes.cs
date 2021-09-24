// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                return /*MM*/NotFound();
            }

            return "Hello world";
        }
    }
}
