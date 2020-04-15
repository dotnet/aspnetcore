// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.Hosting
{
    internal class HostFactoryResolver
    {
        public static readonly string BuildWebHost = nameof(BuildWebHost);
        public static readonly string CreateWebHostBuilder = nameof(CreateWebHostBuilder);
        public static readonly string CreateHostBuilder = nameof(CreateHostBuilder);

        public static Func<string[], TWebHost> ResolveWebHostFactory<TWebHost>(Assembly assembly)
        {
            return ResolveFactory<TWebHost>(assembly, BuildWebHost);
        }

        public static Func<string[], TWebHostBuilder> ResolveWebHostBuilderFactory<TWebHostBuilder>(Assembly assembly)
        {
            return ResolveFactory<TWebHostBuilder>(assembly, CreateWebHostBuilder);
        }

        public static Func<string[], THostBuilder> ResolveHostBuilderFactory<THostBuilder>(Assembly assembly)
        {
            return ResolveFactory<THostBuilder>(assembly, CreateHostBuilder);
        }

        private static Func<string[], T> ResolveFactory<T>(Assembly assembly, string name)
        {
            var programType = assembly?.EntryPoint?.DeclaringType;
            if (programType == null)
            {
                return null;
            }

            var factory = programType.GetTypeInfo().GetDeclaredMethod(name);
            if (!IsFactory<T>(factory))
            {
                return null;
            }

            return args => (T)factory.Invoke(null, new object[] { args });
        }

        // TReturn Factory(string[] args);
        private static bool IsFactory<TReturn>(MethodInfo factory)
        {
            return factory != null
                && typeof(TReturn).IsAssignableFrom(factory.ReturnType)
                && factory.GetParameters().Length == 1
                && typeof(string[]).Equals(factory.GetParameters()[0].ParameterType);
        }

        // Used by EF tooling without any Hosting references. Looses some return type safety checks.
        public static Func<string[], IServiceProvider> ResolveServiceProviderFactory(Assembly assembly)
        {
            // Prefer the older patterns by default for back compat.
            var webHostFactory = ResolveWebHostFactory<object>(assembly);
            if (webHostFactory != null)
            {
                return args =>
                {
                    var webHost = webHostFactory(args);
                    return GetServiceProvider(webHost);
                };
            }

            var webHostBuilderFactory = ResolveWebHostBuilderFactory<object>(assembly);
            if (webHostBuilderFactory != null)
            {
                return args =>
                {
                    var webHostBuilder = webHostBuilderFactory(args);
                    var webHost = Build(webHostBuilder);
                    return GetServiceProvider(webHost);
                };
            }

            var hostBuilderFactory = ResolveHostBuilderFactory<object>(assembly);
            if (hostBuilderFactory != null)
            {
                return args =>
                {
                    var hostBuilder = hostBuilderFactory(args);
                    var host = Build(hostBuilder);
                    return GetServiceProvider(host);
                };
            }

            return null;
        }

        private static object Build(object builder)
        {
            var buildMethod = builder.GetType().GetMethod("Build");
            return buildMethod?.Invoke(builder, Array.Empty<object>());
        }

        private static IServiceProvider GetServiceProvider(object host)
        {
            if (host == null)
            {
                return null;
            }
            var hostType = host.GetType();
            var servicesProperty = hostType.GetTypeInfo().GetDeclaredProperty("Services");
            return (IServiceProvider)servicesProperty.GetValue(host);
        }
    }
}
