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

namespace IISTestSite
{
    public class StartupLargeResponseBody
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async context =>
            {
                if (context.Request.Path.Equals("/LargeResponseBody"))
                {
                    if (int.TryParse(context.Request.Query["length"], out var length))
                    {
                        await context.Response.WriteAsync(new string('a', length));
                    }
                }
                else if (context.Request.Path.Equals("/LargeResponseBodyFromFile"))
                {
                    var fileString = File.ReadAllText("Http.config");
                    await context.Response.WriteAsync(fileString);
                }
            });
        }
    }
}
