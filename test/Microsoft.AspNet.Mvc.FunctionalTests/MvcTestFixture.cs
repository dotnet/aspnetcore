// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Actions;
using Microsoft.AspNet.TestHost;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcFixture : IDisposable
    {
        public MvcFixture(object startupInstance)
        {
            var startupTypeInfo = startupInstance.GetType().GetTypeInfo();
            var configureMethod = (Action<IApplicationBuilder>)startupTypeInfo
                .DeclaredMethods
                .First(m => m.Name == "Configure")
                .CreateDelegate(typeof(Action<IApplicationBuilder>), startupInstance);

            var configureServices = (Action<IServiceCollection>)startupTypeInfo
                .DeclaredMethods
                .First(m => m.Name == "ConfigureServices")
                .CreateDelegate(typeof(Action<IServiceCollection>), startupInstance);

            Server = TestServer.Create(
                CallContextServiceLocator.Locator.ServiceProvider,
                configureMethod,
                configureServices: InitializeServices(startupTypeInfo.Assembly, configureServices));

            Client = Server.CreateClient();
            Client.BaseAddress = new Uri("http://localhost");
        }

        public TestServer Server { get; }

        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }

        public static Func<IServiceCollection, IServiceProvider> InitializeServices(
            Assembly startupAssembly,
            Action<IServiceCollection> configureServices)
        {
            var applicationServices = CallContextServiceLocator.Locator.ServiceProvider;
            var libraryManager = applicationServices.GetRequiredService<ILibraryManager>();

            var applicationName = startupAssembly.GetName().Name;
            var library = libraryManager.GetLibrary(applicationName);
            var applicationRoot = Path.GetDirectoryName(library.Path);

            var applicationEnvironment = applicationServices.GetRequiredService<IApplicationEnvironment>();

            return (services) =>
            {
                services.AddInstance<IApplicationEnvironment>(
                    new TestApplicationEnvironment(applicationEnvironment, applicationName, applicationRoot));

                var hostingEnvironment = new HostingEnvironment();
                hostingEnvironment.Initialize(applicationRoot, "Production");
                services.AddInstance<IHostingEnvironment>(hostingEnvironment);

                var assemblyProvider = new StaticAssemblyProvider();
                assemblyProvider.CandidateAssemblies.Add(startupAssembly);
                services.AddInstance<IAssemblyProvider>(assemblyProvider);

                configureServices(services);

                return services.BuildServiceProvider();
            };
        }

        private class TestApplicationEnvironment : IApplicationEnvironment
        {
            private readonly IApplicationEnvironment _original;

            public TestApplicationEnvironment(IApplicationEnvironment original, string name, string path)
            {
                _original = original;

                ApplicationName = name;
                ApplicationBasePath = path;
            }

            public string ApplicationBasePath { get; }

            public string ApplicationName { get; }

            public string ApplicationVersion => _original.ApplicationVersion;

            public string Configuration => _original.Configuration;

            public FrameworkName RuntimeFramework => _original.RuntimeFramework;

            public object GetData(string name)
            {
                return _original.GetData(name);
            }

            public void SetData(string name, object value)
            {
                _original.SetData(name, value);
            }
        }
    }
}
