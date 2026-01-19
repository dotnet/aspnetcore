// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery.CrossOrigin;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery.Internal;

internal class CrossOriginRequestValidator : ICrossOriginAntiforgery
{
    public CrossOriginValidationResult Validate(HttpContext context)
    {
        throw new NotImplementedException();
    }
}
