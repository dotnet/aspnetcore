// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.Framework.Cache.Distributed;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Session
{
    public class DistributedSessionStore : ISessionStore
    {
        private readonly IDistributedCache _cache;
        private readonly ILoggerFactory _loggerFactory;

        public DistributedSessionStore([NotNull] IDistributedCache cache, [NotNull] ILoggerFactory loggerFactory)
        {
            _cache = cache;
            _loggerFactory = loggerFactory;
        }

        public bool IsAvailable
        {
            get
            {
                return true; // TODO:
            }
        }

        public void Connect()
        {
            _cache.Connect();
        }

        public ISession Create([NotNull] string sessionId, TimeSpan idleTimeout, [NotNull] Func<bool> tryEstablishSession, bool isNewSessionKey)
        {
            return new DistributedSession(_cache, sessionId, idleTimeout, tryEstablishSession, _loggerFactory, isNewSessionKey);
        }
    }
}