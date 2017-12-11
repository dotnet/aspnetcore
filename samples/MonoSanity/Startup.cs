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
            app.UseBlazor(clientAssembly: typeof(MonoSanityClient.Examples).Assembly);
        }
    }
}
