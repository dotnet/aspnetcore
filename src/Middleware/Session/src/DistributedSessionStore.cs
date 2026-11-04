// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Session;

/// <summary>
/// An <see cref="ISessionStore"/> backed by an <see cref="IDistributedCache"/>.
/// </summary>
public class DistributedSessionStore : ISessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedSessionStore"/>.
    /// </summary>
    /// <param name="cache">The <see cref="IDistributedCache"/> used to store the session data.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public DistributedSessionStore(IDistributedCache cache, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _cache = cache;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionKey);
        ArgumentNullException.ThrowIfNull(tryEstablishSession);

        return new DistributedSession(_cache, sessionKey, idleTimeout, ioTimeout, tryEstablishSession, _loggerFactory, isNewSessionKey);
    }
}
