// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
