// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.Hosting;

namespace NegotiateAuthSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (string.Equals("HttpSys", webBuilder.GetSetting("server"), StringComparison.OrdinalIgnoreCase))
                    {
                        webBuilder.UseHttpSys(options =>
                        {
                            options.Authentication.AllowAnonymous = true;
                            options.Authentication.Schemes = AuthenticationSchemes.Negotiate;
                        });
                    }
                    webBuilder.UseStartup<Startup>();
                });
    }
}
