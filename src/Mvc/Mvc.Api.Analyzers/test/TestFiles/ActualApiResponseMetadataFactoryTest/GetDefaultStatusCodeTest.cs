// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    [DefaultStatusCode(StatusCodes.Status412PreconditionFailed)]
    public class TestActionResultUsingStatusCodesConstants { }

    [DefaultStatusCode((int)HttpStatusCode.Redirect)]
    public class TestActionResultUsingHttpStatusCodeCast { }
}
