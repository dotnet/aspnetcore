// -----------------------------------------------------------------------
// <copyright file="ServerFactory.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------
// Copyright 2011-2012 Katana contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;
    using LoggerFactoryFunc = Func<string, Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>>;

    /// <summary>
    /// Implements the Katana setup pattern for this server.
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private LoggerFactoryFunc _loggerFactory;

        public ServerFactory()
        {
            // TODO: Get services from DI, like logger factory.
        }

        /// <summary>
        /// Populates the server capabilities.
        /// Also included is a configurable instance of the server.
        /// </summary>
        /// <param name="properties"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public IServerConfiguration CreateConfiguration()
        {
            ServerConfiguration serverConfig = new ServerConfiguration();
            serverConfig.AdvancedConfiguration = new OwinWebListener();
            return serverConfig;
        }

        /// <summary>
        /// Creates a server and starts listening on the given addresses.
        /// </summary>
        /// <param name="app">The application entry point.</param>
        /// <param name="properties">The configuration.</param>
        /// <returns>The server.  Invoke Dispose to shut down.</returns>
        public IDisposable Start(IServerConfiguration serverConfig, AppFunc app)
        {
            if (serverConfig == null)
            {
                throw new ArgumentNullException("serverConfig");
            }
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            OwinWebListener server = (OwinWebListener)serverConfig.AdvancedConfiguration;

            // TODO: var capabilities = new Dictionary<string, object>();
            WebListenerWrapper wrapper = new WebListenerWrapper(server);

            wrapper.Start(app, serverConfig.Addresses, _loggerFactory);
            return wrapper;
        }
    }
}
