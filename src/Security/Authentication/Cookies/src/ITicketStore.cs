// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies;

/// <summary>
/// This provides an abstract storage mechanic to preserve identity information on the server
/// while only sending a simple identifier key to the client. This is most commonly used to mitigate
/// issues with serializing large identities into cookies.
/// </summary>
public interface ITicketStore
{
    /// <summary>
    /// Store the identity ticket and return the associated key.
    /// </summary>
    /// <param name="ticket">The identity information to store.</param>
    /// <returns>The key that can be used to retrieve the identity later.</returns>
    Task<string> StoreAsync(AuthenticationTicket ticket);

    /// <summary>
    /// Store the identity ticket and return the associated key.
    /// </summary>
    /// <param name="ticket">The identity information to store.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The key that can be used to retrieve the identity later.</returns>
    Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken) => StoreAsync(ticket);

    /// <summary>
    /// Store the identity ticket and return the associated key.
    /// </summary>
    /// <param name="ticket">The identity information to store.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The key that can be used to retrieve the identity later.</returns>
    Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken) => StoreAsync(ticket, cancellationToken);

    /// <summary>
    /// Tells the store that the given identity should be updated.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ticket"></param>
    /// <returns></returns>
    Task RenewAsync(string key, AuthenticationTicket ticket);

    /// <summary>
    /// Tells the store that the given identity should be updated.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ticket"></param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken) => RenewAsync(key, ticket);

    /// <summary>
    /// Tells the store that the given identity should be updated.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="ticket"></param>
    /// <param name="httpContext"></param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task RenewAsync(string key, AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken) => RenewAsync(key, ticket, cancellationToken);

    /// <summary>
    /// Retrieves an identity from the store for the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <returns>The identity associated with the given key, or <c>null</c> if not found.</returns>
    Task<AuthenticationTicket?> RetrieveAsync(string key);

    /// <summary>
    /// Retrieves an identity from the store for the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The identity associated with the given key, or <c>null</c> if not found.</returns>
    Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken) => RetrieveAsync(key);

    /// <summary>
    /// Retrieves an identity from the store for the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns>The identity associated with the given key, or <c>null</c> if not found.</returns>
    Task<AuthenticationTicket?> RetrieveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken) => RetrieveAsync(key, cancellationToken);

    /// <summary>
    /// Remove the identity associated with the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <returns></returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove the identity associated with the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken) => RemoveAsync(key);

    /// <summary>
    /// Remove the identity associated with the given key.
    /// </summary>
    /// <param name="key">The key associated with the identity.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns></returns>
    Task RemoveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken) => RemoveAsync(key, cancellationToken);
}
