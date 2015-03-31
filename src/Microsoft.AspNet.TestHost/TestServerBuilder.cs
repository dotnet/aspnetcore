// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.TestHost
{
    public class TestServerBuilder
    {
        public IServiceProvider FallbackServices { get; set; }
        public string Environment { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationBasePath { get; set; }

        public Type StartupType { get; set; }
        public string StartupAssemblyName { get; set; }
        public IConfiguration Config { get; set; }

        public IServiceCollection AdditionalServices { get; } = new ServiceCollection();

        public StartupMethods Startup { get; set; }

        public TestServer Build()
        {
            var fallbackServices = FallbackServices ?? CallContextServiceLocator.Locator.ServiceProvider;
            var config = Config ?? new Configuration();
            if (Environment != null)
            {
                config[HostingFactory.EnvironmentKey] = Environment;
            }
            if (ApplicationName != null || ApplicationBasePath != null)
            {
                var appEnv = new TestApplicationEnvironment(fallbackServices.GetRequiredService<IApplicationEnvironment>());
                appEnv.ApplicationBasePath = ApplicationBasePath;
                appEnv.ApplicationName = ApplicationName;
                AdditionalServices.AddInstance<IApplicationEnvironment>(appEnv);
            }

            var engine = WebHost.CreateEngine(fallbackServices,
                config,
                services => services.Add(AdditionalServices));
            if (StartupType != null)
            {
                Startup = new StartupLoader(fallbackServices).Load(StartupType, Environment, new List<string>());
            }
            if (Startup != null)
            {
                engine.UseStartup(Startup.ConfigureDelegate, Startup.ConfigureServicesDelegate);
            }
            else if (StartupAssemblyName != null)
            {
                engine.UseStartup(StartupAssemblyName);
            }

            return new TestServer(engine);
        }

        private class TestApplicationEnvironment : IApplicationEnvironment
        {
            private readonly IApplicationEnvironment _appEnv;
            private string _appName;
            private string _appBasePath;

            public TestApplicationEnvironment(IApplicationEnvironment appEnv)
            {
                _appEnv = appEnv;
            }

            public string ApplicationBasePath
            {
                get
                {
                    return _appBasePath ?? _appEnv.ApplicationBasePath;
                }
                set
                {
                    _appBasePath = value;
                }
            }

            public string ApplicationName
            {
                get
                {
                    return _appName ?? _appEnv.ApplicationName;
                }
                set
                {
                    _appName = value;
                }
            }

            public string Configuration
            {
                get
                {
                    return _appEnv.Configuration;
                }
            }

            public FrameworkName RuntimeFramework
            {
                get
                {
                    return _appEnv.RuntimeFramework;
                }
            }

            public string Version
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}