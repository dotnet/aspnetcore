// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Class to configure the <see cref="HubOptions"/>.
/// </summary>
public class HubOptionsSetup : IConfigureOptions<HubOptions>
{
    internal static TimeSpan DefaultHandshakeTimeout => TimeSpan.FromSeconds(15);

    internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

    internal static TimeSpan DefaultClientTimeoutInterval => TimeSpan.FromSeconds(30);

    internal const int DefaultMaximumMessageSize = 32 * 1024;

    internal const int DefaultStreamBufferCapacity = 10;

    private readonly List<string> _defaultProtocols = new List<string>();

    /// <summary>
    /// Constructs the <see cref="HubOptionsSetup"/> with a list of protocols added to Dependency Injection.
    /// </summary>
    /// <param name="protocols">The list of <see cref="IHubProtocol"/>s that are from Dependency Injection.</param>
    public HubOptionsSetup(IEnumerable<IHubProtocol> protocols)
    {
        foreach (var hubProtocol in protocols)
        {
            if (hubProtocol.GetType().CustomAttributes.Where(a => a.AttributeType.FullName == "Microsoft.AspNetCore.SignalR.Internal.NonDefaultHubProtocolAttribute").Any())
            {
                continue;
            }
            _defaultProtocols.Add(hubProtocol.Name);
        }
    }

    /// <summary>
    /// Configures the default values of the <see cref="HubOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="HubOptions"/> to configure.</param>
    public void Configure(HubOptions options)
    {
        if (options.KeepAliveInterval == null)
        {
            // The default keep - alive interval. This is set to exactly half of the default client timeout window,
            // to ensure a ping can arrive in time to satisfy the client timeout.
            options.KeepAliveInterval = DefaultKeepAliveInterval;
        }

        if (options.HandshakeTimeout == null)
        {
            options.HandshakeTimeout = DefaultHandshakeTimeout;
        }

        if (options.MaximumReceiveMessageSize == null)
        {
            options.MaximumReceiveMessageSize = DefaultMaximumMessageSize;
        }

        if (options.SupportedProtocols == null)
        {
            options.SupportedProtocols = new List<string>(_defaultProtocols.Count);
        }

        if (options.StreamBufferCapacity == null)
        {
            options.StreamBufferCapacity = DefaultStreamBufferCapacity;
        }

        foreach (var protocol in _defaultProtocols)
        {
            options.SupportedProtocols.Add(protocol);
        }
    }
}

