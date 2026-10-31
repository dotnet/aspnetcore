// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

/// <summary>
/// State for the Challenge event.
/// </summary>
public class ChallengeContext : PropertiesContext<NegotiateOptions>
{
    /// <summary>
    /// Creates a new <see cref="ChallengeContext"/>.
    /// </summary>
    /// <inheritdoc />
    public ChallengeContext(
        HttpContext context,
        AuthenticationScheme scheme,
        NegotiateOptions options,
        AuthenticationProperties properties)
        : base(context, scheme, options, properties) { }

    /// <summary>
    /// Gets a value that determines if this challenge was handled.
    /// If <see langword="true"/>, will skip any default logic for this challenge.
    /// </summary>
    public bool Handled { get; private set; }

    /// <summary>
    /// Skips any default logic for this challenge.
    /// </summary>
    public void HandleResponse() => Handled = true;
}
