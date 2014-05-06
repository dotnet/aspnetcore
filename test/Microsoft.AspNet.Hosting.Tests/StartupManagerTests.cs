// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Xunit;

namespace Microsoft.AspNet.Hosting
{

    public class StartupManagerTests : IFakeStartupCallback
    {
        private readonly IList<object> _configurationMethodCalledList = new List<object>();

        [Fact]
        public void DefaultServicesLocateStartupByNameAndNamespace()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            var services = serviceCollection.BuildServiceProvider();

            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Fakes.FakeStartup, Microsoft.AspNet.Hosting.Tests");

            Assert.IsType<StartupManager>(manager);
            Assert.NotNull(startup);
        }

        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var manager = services.GetService<IStartupManager>();

            var startup = manager.LoadStartup("Microsoft.AspNet.Hosting.Fakes.FakeStartupWithServices, Microsoft.AspNet.Hosting.Tests");

            startup.Invoke(null);

            Assert.Equal(1, _configurationMethodCalledList.Count);
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
