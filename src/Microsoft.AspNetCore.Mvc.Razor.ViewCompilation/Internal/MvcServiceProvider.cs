// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal class MvcServiceProvider
    {
        private readonly string _projectPath;
        private readonly string _contentRoot;
        private readonly string _applicationName;

        public MvcServiceProvider(
            string projectPath,
            string applicationName,
            string contentRoot,
            string configureCompilationType)
        {
            _projectPath = projectPath;
            _contentRoot = contentRoot;
            _applicationName = applicationName;

            var mvcBuilderConfiguration = GetConfigureCompilationAction(configureCompilationType);
            var serviceProvider = GetProvider(mvcBuilderConfiguration);

            Engine = serviceProvider.GetRequiredService<RazorEngine>();
            TemplateEngine = new MvcRazorTemplateEngine(Engine, serviceProvider.GetRequiredService<RazorProject>())
            {
                Options =
                {
                    ImportsFileName = "_ViewImports.cshtml",
                }
            };
            Compiler = serviceProvider.GetRequiredService<CSharpCompiler>();
            ViewEngineOptions = serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
        }

        public MvcRazorTemplateEngine TemplateEngine { get; }

        public RazorEngine Engine { get; }

        public CSharpCompiler Compiler { get; }

        public RazorViewEngineOptions ViewEngineOptions { get; }

        private IDesignTimeMvcBuilderConfiguration GetConfigureCompilationAction(string configureCompilationType)
        {
            Type type;
            if (!string.IsNullOrEmpty(configureCompilationType))
            {
                type = Type.GetType(configureCompilationType);
                if (type == null)
                {
                    throw new InvalidOperationException($"Unable to find type '{type}.");
                }
            }
            else
            {
                var assemblyName = new AssemblyName(_applicationName);
                var assembly = Assembly.Load(assemblyName);
                type = assembly
                    .GetExportedTypes()
                    .FirstOrDefault(typeof(IDesignTimeMvcBuilderConfiguration).IsAssignableFrom);
            }

            if (type == null)
            {
                return null;
            }

            var instance = Activator.CreateInstance(type) as IDesignTimeMvcBuilderConfiguration;
            if (instance == null)
            {
                throw new InvalidOperationException($"Type {configureCompilationType} does not implement " +
                    $"{typeof(IDesignTimeMvcBuilderConfiguration)}.");
            }

            return instance;
        }

        private IServiceProvider GetProvider(IDesignTimeMvcBuilderConfiguration mvcBuilderConfiguration)
        {
            var services = new ServiceCollection();

            var hostingEnvironment = new HostingEnvironment
            {
                ApplicationName = _applicationName,
                WebRootFileProvider = new PhysicalFileProvider(_projectPath),
                ContentRootFileProvider = new PhysicalFileProvider(_contentRoot),
                ContentRootPath = _contentRoot,
            };
            var diagnosticSource = new DiagnosticListener("Microsoft.AspNetCore");

            services
                .AddSingleton<IHostingEnvironment>(hostingEnvironment)
                .AddSingleton<DiagnosticSource>(diagnosticSource)
                .AddLogging()
                .AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();

            var mvcCoreBuilder = services
                .AddMvcCore()
                .AddRazorViewEngine();

            var mvcBuilder = new MvcBuilder(mvcCoreBuilder.Services, mvcCoreBuilder.PartManager);
            mvcBuilderConfiguration?.ConfigureMvc(mvcBuilder);

            return mvcBuilder.Services.BuildServiceProvider();
        }
    }
}
