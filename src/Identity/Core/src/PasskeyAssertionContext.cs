// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the context for passkey assertion.
/// </summary>
/// <typeparam name="TUser">The type of user associated with the passkey.</typeparam>
public sealed class PasskeyAssertionContext<TUser>
    where TUser : class
{
    /// <summary>
    /// Gets or sets the user associated with the passkey, if known.
    /// </summary>
    public TUser? User { get; init; }

    /// <summary>
    /// Gets or sets the credentials obtained by JSON-serializing the result of the
    /// <c>navigator.credentials.get()</c> JavaScript function.
    /// </summary>
    public required string CredentialJson { get; init; }

    /// <summary>
    /// Gets or sets the JSON representation of the original passkey creation options provided to the browser.
    /// </summary>
    public required string OriginalOptionsJson { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="UserManager{TUser}"/> to retrieve user information from.
    /// </summary>
    public required UserManager<TUser> UserManager { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="HttpContext"/> for the current request. 
    /// </summary>
    public required HttpContext HttpContext { get; init; }
}
