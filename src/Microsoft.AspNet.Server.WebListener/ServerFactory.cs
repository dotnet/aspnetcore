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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Http.Features;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.Logging;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<IFeatureCollection, Task>;

    /// <summary>
    /// Implements the setup process for this server.
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private ILoggerFactory _loggerFactory;

        public ServerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// Creates a configurable instance of the server.
        /// </summary>
        /// <param name="properties"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public IFeatureCollection Initialize(IConfiguration configuration)
        {
            Microsoft.Net.Http.Server.WebListener listener = new Microsoft.Net.Http.Server.WebListener(_loggerFactory);
            ParseAddresses(configuration, listener);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set(listener);
            serverFeatures.Set(new MessagePump(listener, _loggerFactory));
            return serverFeatures;
        }

        /// <summary>
        /// </summary>
        /// <param name="app">The per-request application entry point.</param>
        /// <param name="server">The value returned </param>
        /// <returns>The server.  Invoke Dispose to shut down.</returns>
        public IDisposable Start(IFeatureCollection serverFeatures, AppFunc app)
        {
            if (serverFeatures == null)
            {
                throw new ArgumentNullException("serverFeatures");
            }
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            var messagePump = serverFeatures.Get<MessagePump>();
            if (messagePump == null)
            {
                throw new InvalidOperationException("messagePump");
            }

            messagePump.Start(app);
            return messagePump;
        }

        private void ParseAddresses(IConfiguration config, Microsoft.Net.Http.Server.WebListener listener)
        {
            // TODO: Key format?
            if (config != null && !string.IsNullOrEmpty(config["server.urls"]))
            {
                var urls = config["server.urls"];
                foreach (var value in urls.Split(';'))
                {
                    listener.UrlPrefixes.Add(UrlPrefix.Create(value));
                }
            }
            // TODO: look for just a port option?
        }
    }
}
