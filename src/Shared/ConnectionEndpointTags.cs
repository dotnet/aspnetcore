// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

#nullable enable

internal static class ConnectionEndpointTags
{
    /// <summary>
    /// Adds connection endpoint tags to a TagList using IConnectionEndPointFeature.
    /// </summary>
    /// <param name="tags">The TagList to add tags to.</param>
    /// <param name="features">The feature collection to get endpoint information from.</param>
    public static void AddConnectionEndpointTags(ref TagList tags, IFeatureCollection features)
    {
        var endpointFeature = features.Get<IConnectionEndPointFeature>();
        if (endpointFeature is null)
        {
            return;
        }

        var localEndpoint = endpointFeature.LocalEndPoint;
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

            // There isn't an easy way to detect whether QUIC is the underlying transport.
            // This code assumes that a multiplexed connection is QUIC.
            // Improve in the future if there are additional multiplexed connection types.
            // Note: We can't determine transport from features alone, so we default to "tcp"
            tags.Add("network.transport", "tcp");
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

    /// <summary>
    /// Adds connection endpoint tags to a TagList using IConnectionEndPointFeature.
    /// </summary>
    /// <param name="tags">The TagList to add tags to.</param>
    /// <param name="connectionContext">The connection context to get endpoint information from.</param>
    public static void AddConnectionEndpointTags(ref TagList tags, BaseConnectionContext connectionContext)
    {
        var endpointFeature = connectionContext.Features.Get<IConnectionEndPointFeature>();
        if (endpointFeature is null)
        {
            return;
        }

        var localEndpoint = endpointFeature.LocalEndPoint;
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

            // There isn't an easy way to detect whether QUIC is the underlying transport.
            // This code assumes that a multiplexed connection is QUIC.
            // Improve in the future if there are additional multiplexed connection types.
            var transport = connectionContext is not MultiplexedConnectionContext ? "tcp" : "udp";
            tags.Add("network.transport", transport);
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