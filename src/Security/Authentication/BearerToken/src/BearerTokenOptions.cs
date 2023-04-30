// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

/// <summary>
/// Contains the options used to authenticate using opaque bearer tokens.
/// </summary>
public sealed class BearerTokenOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Constructs the options used to authenticate using opaque bearer tokens.
    /// </summary>
    public BearerTokenOptions()
    {
        Events = new();
    }

    /// <summary>
    /// Controls how much time the bearer token will remain valid from the point it is created.
    /// The expiration information is stored in the protected token. Because of that, an expired token will be rejected
    /// even if it is passed to the server after the client should have purged it.
    /// </summary>
    public TimeSpan BearerTokenExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// If set, the <see cref="BearerTokenProtector"/> is used to protect and unprotect the identity and other properties which are stored in the
    /// bearer token value. If not provided, one will be created using <see cref="TicketDataFormat"/> and the <see cref="IDataProtectionProvider"/>
    /// from the application <see cref="IServiceProvider"/>.
    /// </summary>
    public ISecureDataFormat<AuthenticationTicket>? BearerTokenProtector { get; set; }

    /// <summary>
    /// The object provided by the application to process events raised by the bearer token authentication handler.
    /// The application may implement the interface fully, or it may create an instance of <see cref="BearerTokenEvents"/>
    /// and assign delegates only to the events it wants to process.
    /// </summary>
    public new BearerTokenEvents Events
    {
        get { return (BearerTokenEvents)base.Events!; }
        set { base.Events = value; }
    }
}

