// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Server.Kestrel;

namespace Microsoft.AspNetCore.Hosting
{
    public static class KestrelWebHostBuilderExtensions
    {
        public static IWebHostBuilder UseKestrel(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.UseServer(typeof(KestrelServer).GetTypeInfo().Assembly.FullName);
        }
    }
}
