// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace IISTestSite
{
    public class StartupResponseHeaders
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/ResponseHeaders"))
                {
                    context.Response.Headers["UnknownHeader"] = "test123=foo";
                    context.Response.ContentType = "text/plain";
                    context.Response.Headers["MultiHeader"] = new StringValues(new string[] { "1", "2" });
                    await context.Response.WriteAsync("Request Complete");
                }
            });
        }
    }
}
