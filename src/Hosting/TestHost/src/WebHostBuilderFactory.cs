// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WebHostBuilderFactory;

namespace Microsoft.AspNetCore.TestHost
{
    public static class WebHostBuilderFactory
    {
        public static IWebHostBuilder CreateFromAssemblyEntryPoint(Assembly assembly, string [] args)
        {
            var result = WebHostFactoryResolver.ResolveWebHostBuilderFactory<IWebHost,IWebHostBuilder>(assembly);
            if (result.ResultKind != FactoryResolutionResultKind.Success)
            {
                return null;
            }

            return result.WebHostBuilderFactory(args);
        }

        public static IWebHostBuilder CreateFromTypesAssemblyEntryPoint<T>(string[] args) =>
            CreateFromAssemblyEntryPoint(typeof(T).Assembly, args);
    }
}
