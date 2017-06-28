// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Session
{
    public class DistributedSessionStore : ISessionStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILoggerFactory _loggerFactory;

        public DistributedSessionStore(IDistributedCache cache, ILoggerFactory loggerFactory)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _cache = cache;
            _loggerFactory = loggerFactory;
        }

        public ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey)
        {
            if (string.IsNullOrEmpty(sessionKey))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(sessionKey));
            }

            if (tryEstablishSession == null)
            {
                throw new ArgumentNullException(nameof(tryEstablishSession));
            }

            return new DistributedSession(_cache, sessionKey, idleTimeout, ioTimeout, tryEstablishSession, _loggerFactory, isNewSessionKey);
        }
    }
}