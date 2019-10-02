// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.TestHost
{
    public static class WebHostBuilderFactory
    {
        public static IWebHostBuilder CreateFromAssemblyEntryPoint(Assembly assembly, string[] args)
        {
            var factory = HostFactoryResolver.ResolveWebHostBuilderFactory<IWebHostBuilder>(assembly);
            return factory?.Invoke(args);
        }

        public static IWebHostBuilder CreateFromTypesAssemblyEntryPoint<T>(string[] args) =>
            CreateFromAssemblyEntryPoint(typeof(T).Assembly, args);
    }
}
