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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupManager : IStartupManager
    {
        private readonly IEnumerable<IStartupLoaderProvider> _providers;

        public StartupManager(IEnumerable<IStartupLoaderProvider> providers)
        {
            _providers = providers;
        }

        public Action<IBuilder> LoadStartup(string applicationName)
        {
            // build ordered chain of application loaders
            var chain = _providers
                .OrderBy(provider => provider.Order)
                .Aggregate(NullStartupLoader.Instance, (next, provider) => provider.CreateStartupLoader(next));

            // invoke chain to acquire application entrypoint and diagnostic messages
            var diagnosticMessages = new List<string>();
            var application = chain.LoadStartup(applicationName, diagnosticMessages);

            if (application == null)
            {
                throw new Exception(diagnosticMessages.Aggregate("TODO: web application entrypoint not found message", (a, b) => a + "\r\n" + b));
            }

            return application;
        }
    }
}