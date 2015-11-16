// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    public class PlainController
    {
        [ActionContext]
        public ActionContext ActionContext { get; set; }

        public HttpRequest Request => ActionContext.HttpContext.Request;

        public HttpResponse Response => ActionContext.HttpContext.Response;

        public IActionResult Index([FromServices] MyService service)
        {
            Response.Headers["X-Fake-Header"] = "Fake-Value";

            var value = Request.Query["foo"];
            return new ContentResult { Content = service.Random + "|" + value };
        }
    }
}