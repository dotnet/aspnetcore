// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Server.Kestrel;

/// <summary>
/// The configuration for an endpoint.
/// </summary>
public class EndpointConfiguration
{
    internal EndpointConfiguration(bool isHttps, ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions, IConfigurationSection configSection)
    {
        IsHttps = isHttps;
        ListenOptions = listenOptions ?? throw new ArgumentNullException(nameof(listenOptions));
        HttpsOptions = httpsOptions ?? throw new ArgumentNullException(nameof(httpsOptions));
        ConfigSection = configSection ?? throw new ArgumentNullException(nameof(configSection));
    }

    /// <summary>
    /// Gets whether the endpoint uses HTTPS.
    /// </summary>
    public bool IsHttps { get; }

    /// <summary>
    /// Gets the endpoint <see cref="ListenOptions"/>.
    /// </summary>
    public ListenOptions ListenOptions { get; }

    /// <summary>
    /// Gets the <see cref="HttpsConnectionAdapterOptions"/>.
    /// </summary>
    public HttpsConnectionAdapterOptions HttpsOptions { get; }

    /// <summary>
    /// Gets the <see cref="IConfigurationSection"/> for the endpoint.
    /// </summary>
    public IConfigurationSection ConfigSection { get; }
}
