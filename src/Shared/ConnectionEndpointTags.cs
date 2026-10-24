// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

#nullable enable

namespace Microsoft.AspNetCore.Shared;

internal static class ConnectionEndpointTags
{
    /// <summary>
    /// Adds connection endpoint tags to a collection of tags, such as a <see cref="TagList"/>, using <see cref="IConnectionEndPointFeature"/>.
    /// </summary>
    /// <param name="tags">The tags to add to.</param>
    /// <param name="features">The feature collection to get endpoint information from.</param>
    public static void AddConnectionEndpointTags<TTags>(ref TTags tags, IFeatureCollection features)
        where TTags : ICollection<KeyValuePair<string, object?>>
    {
        var endpointFeature = features.Get<IConnectionEndPointFeature>();
        if (endpointFeature is null)
        {
            return;
        }

        // This overload only has endpoint information from the feature collection and does not attempt
        // to infer whether the underlying transport is multiplexed or QUIC. For IP endpoints, it records
        // the transport as TCP.
        AddEndpointTags(ref tags, endpointFeature.LocalEndPoint, networkTransport: "tcp");
    }

    /// <summary>
    /// Adds connection endpoint tags to a collection of tags, such as a <see cref="TagList"/>, using <see cref="IConnectionEndPointFeature"/>,
    /// with a fallback to the <see cref="BaseConnectionContext"/> endpoint properties.
    /// </summary>
    /// <param name="tags">The tags to add to.</param>
    /// <param name="connectionContext">The connection context to get endpoint information from.</param>
    public static void AddConnectionEndpointTags<TTags>(ref TTags tags, BaseConnectionContext connectionContext)
        where TTags : ICollection<KeyValuePair<string, object?>>
    {
        // Try to get the local endpoint from the feature first, then fall back to the direct property.
        var localEndpoint = connectionContext.Features.Get<IConnectionEndPointFeature>()?.LocalEndPoint
            ?? connectionContext.LocalEndPoint;

        // There isn't an easy way to detect whether QUIC is the underlying transport.
        // This code assumes that a multiplexed connection is QUIC.
        // Improve in the future if there are additional multiplexed connection types.
        var networkTransport = connectionContext is not MultiplexedConnectionContext ? "tcp" : "udp";
        AddEndpointTags(ref tags, localEndpoint, networkTransport);
    }

    // Generic so value types like TagList are updated in place without boxing.
    private static void AddEndpointTags<TTags>(ref TTags tags, EndPoint? localEndpoint, string networkTransport)
        where TTags : ICollection<KeyValuePair<string, object?>>
    {
        if (localEndpoint is IPEndPoint localIPEndPoint)
        {
            tags.Add(new("server.address", localIPEndPoint.Address.ToString()));
            tags.Add(new("server.port", localIPEndPoint.Port));

            switch (localIPEndPoint.Address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    tags.Add(new("network.type", "ipv4"));
                    break;
                case AddressFamily.InterNetworkV6:
                    tags.Add(new("network.type", "ipv6"));
                    break;
            }

            tags.Add(new("network.transport", networkTransport));
        }
        else if (localEndpoint is UnixDomainSocketEndPoint udsEndPoint)
        {
            tags.Add(new("server.address", udsEndPoint.ToString()));
            tags.Add(new("network.transport", "unix"));
        }
        else if (localEndpoint is NamedPipeEndPoint namedPipeEndPoint)
        {
            tags.Add(new("server.address", namedPipeEndPoint.ToString()));
            tags.Add(new("network.transport", "pipe"));
        }
        else if (localEndpoint != null)
        {
            tags.Add(new("server.address", localEndpoint.ToString()));
            tags.Add(new("network.transport", localEndpoint.AddressFamily.ToString()));
        }
    }
}
