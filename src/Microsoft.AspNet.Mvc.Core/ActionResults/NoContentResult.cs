// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNet.Mvc
{
    public class NoContentResult : ActionResult
    {
        public override void ExecuteResult([NotNull] ActionContext context)
        {
            var response = context.HttpContext.Response;

#if ASPNET50
            response.StatusCode = (int)HttpStatusCode.NoContent;
#else
            response.StatusCode = 204;
#endif
        }
    }
}
