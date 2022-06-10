// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides an interface for implmenting a construct that provides
/// access to authentication-related configuration sections.
/// </summary>
public interface IAuthenticationConfigurationProvider
{
    /// <summary>
    /// Gets the <see cref="ConfigurationSection"/> where authentication
    /// options are stored.
    /// </summary>
    IConfiguration AuthenticationConfiguration { get; }
}
