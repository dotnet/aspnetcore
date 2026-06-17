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
    /// Adds connection endpoint tags to a TagList using <see cref="IConnectionEndPointFeature"/>.
    /// </summary>
    /// <param name="tags">The <see cref="TagList"/> to add tags to.</param>
    /// <param name="features">The feature collection to get endpoint information from.</param>
    public static void AddConnectionEndpointTags(ref TagList tags, IFeatureCollection features)
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
    /// Adds connection endpoint tags to a TagList using <see cref="IConnectionEndPointFeature"/>,
    /// with a fallback to the <see cref="BaseConnectionContext"/> endpoint properties.
    /// </summary>
    /// <param name="tags">The <see cref="TagList"/> to add tags to.</param>
    /// <param name="connectionContext">The connection context to get endpoint information from.</param>
    public static void AddConnectionEndpointTags(ref TagList tags, BaseConnectionContext connectionContext)
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

    private static void AddEndpointTags(ref TagList tags, EndPoint? localEndpoint, string networkTransport)
    {
        if (localEndpoint is IPEndPoint localIPEndPoint)
        {
            tags.Add("server.address", localIPEndPoint.Address.ToString());
            tags.Add("server.port", localIPEndPoint.Port);

            switch (localIPEndPoint.Address.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    tags.Add("network.type", "ipv4");
                    break;
                case AddressFamily.InterNetworkV6:
                    tags.Add("network.type", "ipv6");
                    break;
            }

            tags.Add("network.transport", networkTransport);
        }
        else if (localEndpoint is UnixDomainSocketEndPoint udsEndPoint)
        {
            tags.Add("server.address", udsEndPoint.ToString());
            tags.Add("network.transport", "unix");
        }
        else if (localEndpoint is NamedPipeEndPoint namedPipeEndPoint)
        {
            tags.Add("server.address", namedPipeEndPoint.ToString());
            tags.Add("network.transport", "pipe");
        }
        else if (localEndpoint != null)
        {
            tags.Add("server.address", localEndpoint.ToString());
            tags.Add("network.transport", localEndpoint.AddressFamily.ToString());
        }
    }
}
