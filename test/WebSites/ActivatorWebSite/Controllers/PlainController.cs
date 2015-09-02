// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ActionResults;
using Microsoft.AspNet.Mvc.Actions;

namespace ActivatorWebSite
{
    public class PlainController
    {
        [FromServices]
        public MyService Service { get; set; }

        [ActionContext]
        public ActionContext ActionContext { get; set; }

        public HttpRequest Request => ActionContext.HttpContext.Request;

        public HttpResponse Response => ActionContext.HttpContext.Response;

        public IActionResult Index()
        {
            Response.Headers["X-Fake-Header"] = "Fake-Value";

            var value = Request.Query["foo"];
            return new ContentResult { Content = Service.Random + "|" + value };
        }
    }
}