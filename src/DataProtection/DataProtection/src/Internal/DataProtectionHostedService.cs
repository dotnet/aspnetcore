// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal partial class DataProtectionHostedService : IHostedService
    {
        private readonly IKeyRingProvider _keyRingProvider;
        private readonly ILogger<DataProtectionHostedService> _logger;

        public DataProtectionHostedService(IKeyRingProvider keyRingProvider)
            : this(keyRingProvider, NullLoggerFactory.Instance)
        { }

        public DataProtectionHostedService(IKeyRingProvider keyRingProvider, ILoggerFactory loggerFactory)
        {
            _keyRingProvider = keyRingProvider;
            _logger = loggerFactory.CreateLogger<DataProtectionHostedService>();
        }

        public Task StartAsync(CancellationToken token)
        {
            try
            {
                // It doesn't look like much, but this preloads the key ring,
                // which in turn may load data from remote stores like Redis or Azure.
                var keyRing = _keyRingProvider.GetCurrentKeyRing();
                Log.KeyRingWasLoadedOnStartup(_logger, keyRing.DefaultKeyId);
            }
            catch (Exception ex)
            {
                Log.KeyRingFailedToLoadOnStartup(
                                // This should be non-fatal, so swallow, log, and allow server startup to continue.
                                // The KeyRingProvider may be able to try again on the first request.
                                _logger, ex);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token) => Task.CompletedTask;

        private partial class Log
        {
            [LoggerMessage(65, LogLevel.Debug, "Key ring with default key {KeyId:B} was loaded during application startup.", EventName = "KeyRingWasLoadedOnStartup")]
            public static partial void KeyRingWasLoadedOnStartup(ILogger logger, Guid keyId);

            [LoggerMessage(66, LogLevel.Information, "Key ring failed to load during application startup.", EventName = "KeyRingFailedToLoadOnStartup")]
            public static partial void KeyRingFailedToLoadOnStartup(ILogger logger, Exception innerException);
        }
    }
}
