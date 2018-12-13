// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISSetupFilter : IStartupFilter
    {
        private readonly string _pairingToken;
        private readonly PathString _pathBase;
        private readonly bool _isWebsocketsSupported;
        private readonly string _serverAddresses;

        internal IISSetupFilter(string pairingToken, PathString pathBase, bool isWebsocketsSupported, string serverAddresses)
        {
            _pairingToken = pairingToken;
            _pathBase = pathBase;
            _isWebsocketsSupported = isWebsocketsSupported;
            _serverAddresses = serverAddresses;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UsePathBase(_pathBase);
                app.UseForwardedHeaders();
                app.UseMiddleware<IISMiddleware>(_pairingToken, _isWebsocketsSupported);
                if (_serverAddresses != null)
                {
                    app.ServerFeatures.Set<IServerAddressesFeature>(new ServerAddressesFeature(_serverAddresses));
                }
                next(app);
            };
        }

        internal class ServerAddressesFeature : IServerAddressesFeature
        {
            public ServerAddressesFeature(string addresses)
            {
                Addresses = addresses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            }

            public ICollection<string> Addresses { get; }
            public bool PreferHostingUrls { get; set; }
        }
    }
}
