// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Class to configure the <see cref="HubOptions"/> for a specific <typeparamref name="THub"/>.
/// </summary>
/// <typeparam name="THub">The <see cref="Hub"/> type to configure.</typeparam>
public class HubOptionsSetup<THub> : IConfigureOptions<HubOptions<THub>> where THub : Hub
{
    private readonly HubOptions _hubOptions;

    /// <summary>
    /// Constructs the options configuration class.
    /// </summary>
    /// <param name="options">The global <see cref="HubOptions"/> from Dependency Injection.</param>
    public HubOptionsSetup(IOptions<HubOptions> options)
    {
        _hubOptions = options.Value;
    }

    /// <summary>
    /// Configures the default values of the <see cref="HubOptions"/>.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    public void Configure(HubOptions<THub> options)
    {
        // Do a deep copy, otherwise users modifying the HubOptions<THub> list would be changing the global options list
        options.SupportedProtocols = new List<string>(_hubOptions.SupportedProtocols ?? Array.Empty<string>());
        options.KeepAliveInterval = _hubOptions.KeepAliveInterval;
        options.HandshakeTimeout = _hubOptions.HandshakeTimeout;
        options.ClientTimeoutInterval = _hubOptions.ClientTimeoutInterval;
        options.EnableDetailedErrors = _hubOptions.EnableDetailedErrors;
        options.MaximumReceiveMessageSize = _hubOptions.MaximumReceiveMessageSize;
        options.StreamBufferCapacity = _hubOptions.StreamBufferCapacity;
        options.MaximumParallelInvocationsPerClient = _hubOptions.MaximumParallelInvocationsPerClient;
        options.DisableImplicitFromServicesParameters = _hubOptions.DisableImplicitFromServicesParameters;

        options.UserHasSetValues = true;

        if (_hubOptions.HubFilters != null)
        {
            options.HubFilters = new List<IHubFilter>(_hubOptions.HubFilters);
        }
    }
}
