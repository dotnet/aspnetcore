// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Session;

/// <summary>
/// Storage for sessions that maintain user data while the user browses a web application.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Create a new or resume an <see cref="ISession"/>.
    /// </summary>
    /// <param name="sessionKey">A unique key used to lookup the session.</param>
    /// <param name="idleTimeout">How long the session can be inactive (e.g. not accessed) before it will expire.</param>
    /// <param name="ioTimeout">
    /// The maximum amount of time <see cref="ISession.LoadAsync(System.Threading.CancellationToken)"/> and
    /// <see cref="ISession.CommitAsync(System.Threading.CancellationToken)"/> are allowed take.
    /// </param>
    /// <param name="tryEstablishSession">
    /// A callback invoked during <see cref="ISession.Set(string, byte[])"/> to verify that modifying the session is currently valid.
    /// If the callback returns <see langword="false"/>, <see cref="ISession.Set(string, byte[])"/> should throw an <see cref="InvalidOperationException"/>.
    /// <see cref="SessionMiddleware"/> provides a callback that returns <see langword="false"/> if the session was not established
    /// prior to sending the response.
    /// </param>
    /// <param name="isNewSessionKey"><see langword="true"/> if establishing a new session; <see langword="false"/> if resuming a session.</param>
    /// <returns>The <see cref="ISession"/> that was created or resumed.</returns>
    ISession Create(string sessionKey, TimeSpan idleTimeout, TimeSpan ioTimeout, Func<bool> tryEstablishSession, bool isNewSessionKey);
}
