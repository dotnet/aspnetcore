// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Blazor.Hosting;

namespace BasicTestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // We want the culture to be en-US so that the tests for bind can work consistently.
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            CreateHostBuilder(args).Build().Run();
        }

        public static IWebAssemblyHostBuilder CreateHostBuilder(string[] args) =>
            BlazorWebAssemblyHost.CreateDefaultBuilder()
                .UseBlazorStartup<Startup>();
    }
}
