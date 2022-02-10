// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

/// <summary>
///     Entity type for a user's login (i.e. facebook, google)
/// </summary>
public class PocoUserLogin : PocoUserLogin<string> { }

/// <summary>
///     Entity type for a user's login (i.e. facebook, google)
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class PocoUserLogin<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    ///     The login provider for the login (i.e. facebook, google)
    /// </summary>
    public virtual string LoginProvider { get; set; }

    /// <summary>
    ///     Key representing the login for the provider
    /// </summary>
    public virtual string ProviderKey { get; set; }

    /// <summary>
    ///     Display name for the login
    /// </summary>
    public virtual string ProviderDisplayName { get; set; }

    /// <summary>
    ///     User Id for the user who owns this login
    /// </summary>
    public virtual TKey UserId { get; set; }
}
