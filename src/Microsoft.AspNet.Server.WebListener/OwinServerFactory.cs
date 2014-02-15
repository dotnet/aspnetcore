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

namespace Microsoft.AspNet.Server.WebListener
{
    using AppFunc = Func<object, Task>;
    using LoggerFactoryFunc = Func<string, Func<TraceEventType, int, object, Exception, Func<object, Exception, string>, bool>>;

    /// <summary>
    /// Implements the Katana setup pattern for this server.
    /// </summary>
    public static class OwinServerFactory 
    {
        /// <summary>
        /// Populates the server capabilities.
        /// Also included is a configurable instance of the server.
        /// </summary>
        /// <param name="properties"></param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public static void Initialize(IDictionary<string, object> properties)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            properties[Constants.VersionKey] = Constants.OwinVersion;

            IDictionary<string, object> capabilities =
                properties.Get<IDictionary<string, object>>(Constants.ServerCapabilitiesKey)
                    ?? new Dictionary<string, object>();
            properties[Constants.ServerCapabilitiesKey] = capabilities;
            
            // SendFile
            capabilities[Constants.SendFileVersionKey] = Constants.SendFileVersion;
            IDictionary<string, object> sendfileSupport = new Dictionary<string, object>();
            sendfileSupport[Constants.SendFileConcurrencyKey] = Constants.Overlapped;
            capabilities[Constants.SendFileSupportKey] = sendfileSupport;

            // Opaque
            if (ComNetOS.IsWin8orLater)
            {
                capabilities[Constants.OpaqueVersionKey] = Constants.OpaqueVersion;
            }

            // Directly expose the server for advanced configuration.
            properties[typeof(OwinWebListener).FullName] = new OwinWebListener();
        }

        /// <summary>
        /// Creates a server and starts listening on the given addresses.
        /// </summary>
        /// <param name="app">The application entry point.</param>
        /// <param name="properties">The configuration.</param>
        /// <returns>The server.  Invoke Dispose to shut down.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "By design")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by caller")]
        public static IDisposable Create(AppFunc app, IDictionary<string, object> properties)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            var addresses = properties.Get<IList<IDictionary<string, object>>>("host.Addresses")
                ?? new List<IDictionary<string, object>>();

            OwinWebListener server = properties.Get<OwinWebListener>(typeof(OwinWebListener).FullName)
                ?? new OwinWebListener();

            var capabilities =
                properties.Get<IDictionary<string, object>>(Constants.ServerCapabilitiesKey)
                    ?? new Dictionary<string, object>();

            var loggerFactory = properties.Get<LoggerFactoryFunc>(Constants.ServerLoggerFactoryKey);

            server.Start(app, addresses, capabilities, loggerFactory);
            return server;
        }
    }
}
