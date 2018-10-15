// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal class DataProtectionStartupFilter : IStartupFilter
    {
        private readonly IKeyRingProvider _keyRingProvider;
        private readonly ILogger<DataProtectionStartupFilter> _logger;

        public DataProtectionStartupFilter(IKeyRingProvider keyRingProvider)
            : this(keyRingProvider, NullLoggerFactory.Instance)
        { }

        public DataProtectionStartupFilter(IKeyRingProvider keyRingProvider, ILoggerFactory loggerFactory)
        {
            _keyRingProvider = keyRingProvider;
            _logger = loggerFactory.CreateLogger<DataProtectionStartupFilter>();
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            try
            {
                // It doesn't look like much, but this preloads the key ring,
                // which in turn may load data from remote stores like Redis or Azure.
                var keyRing = _keyRingProvider.GetCurrentKeyRing();

                _logger.KeyRingWasLoadedOnStartup(keyRing.DefaultKeyId);
            }
            catch (Exception ex)
            {
                // This should be non-fatal, so swallow, log, and allow server startup to continue.
                // The KeyRingProvider may be able to try again on the first request.
                _logger.KeyRingFailedToLoadOnStartup(ex);
            }

            return next;
        }
    }
}
