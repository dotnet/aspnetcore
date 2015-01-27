// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace ActivatorWebSite
{
    public class PlainController
    {
        [FromServices]
        public MyService Service { get; set; }

        [Activate]
        public HttpRequest Request { get; set; }

        [Activate]
        public HttpResponse Response { get; set; }

        public IActionResult Index()
        {
            Response.Headers["X-Fake-Header"] = "Fake-Value";

            var value = Request.Query["foo"];
            return new ContentResult { Content = Service.Random + "|" + value };
        }
    }
}