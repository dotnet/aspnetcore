// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using MonoSanityClient;
using System.IO;
using System.Net.Mime;

namespace MonoSanity
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();
            app.UseFileServer();
            app.UseBlazor();
            
            ServeSingleStaticFile(app, 
                "/clientBin/MonoSanityClient.dll",
                typeof(Examples).Assembly.Location);            
        }

        private void ServeSingleStaticFile(IApplicationBuilder app, string url, string physicalPath)
        {
            // This is not implemented efficiently (e.g., doesn't support cache control headers)
            // so don't use this in real applications. Use 'UseStaticFiles' or similar instead.
            app.Use((context, next) =>
            {
                if (context.Request.Path == url)
                {
                    context.Response.ContentType = MediaTypeNames.Application.Octet;
                    return File.OpenRead(physicalPath).CopyToAsync(context.Response.Body);
                }
                else
                {
                    return next();
                }
            });
        }
    }
}
