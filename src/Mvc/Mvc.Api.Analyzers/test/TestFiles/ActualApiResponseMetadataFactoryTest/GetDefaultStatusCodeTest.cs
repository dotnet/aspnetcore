// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
