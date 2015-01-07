// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.DependencyInjection.ServiceLookup;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc
{
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private readonly IServiceProvider _appServices;

        public RazorPreCompileModule(IServiceProvider services)
        {
            _appServices = services;
        }

        protected virtual string FileExtension { get; } = ".cshtml";

        public virtual void BeforeCompile(IBeforeCompileContext context)
        {
            var applicationEnvironment = _appServices.GetRequiredService<IApplicationEnvironment>();
            var compilerOptionsProvider = _appServices.GetRequiredService<ICompilerOptionsProvider>();
            var compilationSettings = compilerOptionsProvider.GetCompilationSettings(applicationEnvironment);

            var setup = new RazorViewEngineOptionsSetup(applicationEnvironment);
            var sc = new ServiceCollection();
            sc.ConfigureOptions(setup);
            sc.AddMvc();

            var serviceProvider = BuildFallbackServiceProvider(sc, _appServices);
            var viewCompiler = new RazorPreCompiler(serviceProvider, compilationSettings);
            viewCompiler.CompileViews(context);
        }

        public void AfterCompile(IAfterCompileContext context)
        {
        }

        // TODO: KILL THIS
        private static IServiceProvider BuildFallbackServiceProvider(
            IEnumerable<IServiceDescriptor> services,
            IServiceProvider fallback)
        {
            var sc = HostingServices.Create(fallback);
            sc.Add(services);

            // Build the manifest
            var manifestTypes = services.Where(t => t.ServiceType.GetTypeInfo().GenericTypeParameters.Length == 0
                    && t.ServiceType != typeof(IServiceManifest)
                    && t.ServiceType != typeof(IServiceProvider))
                    .Select(t => t.ServiceType).Distinct();
            sc.AddInstance<IServiceManifest>(
                new ServiceManifest(manifestTypes, fallback.GetRequiredService<IServiceManifest>()));
            return sc.BuildServiceProvider();
        }

        private class ServiceManifest : IServiceManifest
        {
            public ServiceManifest(IEnumerable<Type> services, IServiceManifest fallback = null)
            {
                Services = services;
                if (fallback != null)
                {
                    Services = Services.Concat(fallback.Services).Distinct();
                }
            }

            public IEnumerable<Type> Services { get; private set; }
        }
    }
}
