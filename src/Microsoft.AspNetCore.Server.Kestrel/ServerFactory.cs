// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    /// <summary>
    /// Summary description for ServerFactory
    /// </summary>
    public class ServerFactory : IServerFactory
    {
        private readonly IApplicationLifetime _appLifetime;
        private readonly ILoggerFactory _loggerFactory;
        private readonly KestrelServerOptions _options;

        public ServerFactory(IApplicationLifetime appLifetime, ILoggerFactory loggerFactory, IOptions<KestrelServerOptions> optionsAccessor)
        {
            _appLifetime = appLifetime;
            _loggerFactory = loggerFactory;
            _options = optionsAccessor.Value;
        }

        public IServer CreateServer(IConfiguration configuration)
        {
            var componentFactory = new HttpComponentFactory(_options);
            var serverFeatures = new FeatureCollection();
            serverFeatures.Set<IHttpComponentFactory>(componentFactory);
            serverFeatures.Set(GetAddresses(configuration));
            return new KestrelServer(serverFeatures, _options, _appLifetime, _loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel"));
        }

        private static IServerAddressesFeature GetAddresses(IConfiguration configuration)
        {
            var addressesFeature = new ServerAddressesFeature();
            var urls = configuration["server.urls"];
            if (!string.IsNullOrEmpty(urls))
            {
                foreach (var value in urls.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    addressesFeature.Addresses.Add(value);
                }
            }
            return addressesFeature;
        }

    }
}
