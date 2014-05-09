// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class HttpStatusCodeResult : ActionResult
    {
        private int _statusCode;

        public HttpStatusCodeResult(int statusCode)
        {
            _statusCode = statusCode;
        }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            context.HttpContext.Response.StatusCode = _statusCode;
        }
    }
}
