// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    public class NoContentResult : ActionResult
    {
        public override void ExecuteResult([NotNull] ActionContext context)
        {
            HttpResponse response = context.HttpContext.Response;

#if NET45
            response.StatusCode = (int)HttpStatusCode.NoContent;
#else
            response.StatusCode = 204;
#endif
        }
    }
}
