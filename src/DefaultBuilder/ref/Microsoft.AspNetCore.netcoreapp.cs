// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore
{
    public static partial class WebHost
    {
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateDefaultBuilder() { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateDefaultBuilder(string[] args) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHostBuilder CreateDefaultBuilder<TStartup>(string[] args) where TStartup : class { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost Start(Microsoft.AspNetCore.Http.RequestDelegate app) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost Start(System.Action<Microsoft.AspNetCore.Routing.IRouteBuilder> routeBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost Start(string url, Microsoft.AspNetCore.Http.RequestDelegate app) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost Start(string url, System.Action<Microsoft.AspNetCore.Routing.IRouteBuilder> routeBuilder) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost StartWith(System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> app) { throw null; }
        public static Microsoft.AspNetCore.Hosting.IWebHost StartWith(string url, System.Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> app) { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting
{
    public static partial class GenericHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder ConfigureWebHostDefaults(this Microsoft.Extensions.Hosting.IHostBuilder builder, System.Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder> configure) { throw null; }
    }
}
