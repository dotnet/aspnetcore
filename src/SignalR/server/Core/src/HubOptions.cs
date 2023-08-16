// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Options used to configure hub instances.
/// </summary>
public class HubOptions
{
    private int _maximumParallelInvocationsPerClient = 1;

    // HandshakeTimeout and KeepAliveInterval are set to null here to help identify when
    // local hub options have been set. Global default values are set in HubOptionsSetup.
    // SupportedProtocols being null is the true default value, and it represents support
    // for all available protocols.

    /// <summary>
    /// Gets or sets the interval used by the server to timeout incoming handshake requests by clients. The default timeout is 15 seconds.
    /// </summary>
    public TimeSpan? HandshakeTimeout { get; set; }

    /// <summary>
    /// Gets or sets the interval used by the server to send keep alive pings to connected clients. The default interval is 15 seconds.
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    /// Gets or sets the time window clients have to send a message before the server closes the connection. The default timeout is 30 seconds.
    /// </summary>
    public TimeSpan? ClientTimeoutInterval { get; set; }

    /// <summary>
    /// Gets or sets a collection of supported hub protocol names.
    /// </summary>
    public IList<string>? SupportedProtocols { get; set; }

    /// <summary>
    /// Gets or sets the maximum message size of a single incoming hub message. The default is 32KB.
    /// </summary>
    public long? MaximumReceiveMessageSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether detailed error messages are sent to the client.
    /// Detailed error messages include details from exceptions thrown on the server.
    /// </summary>
    public bool? EnableDetailedErrors { get; set; }

    /// <summary>
    /// Gets or sets the max buffer size for client upload streams. The default size is 10.
    /// </summary>
    public int? StreamBufferCapacity { get; set; }

    internal List<IHubFilter>? HubFilters { get; set; }

    /// <summary>
    /// By default a client is only allowed to invoke a single Hub method at a time.
    /// Changing this property will allow clients to invoke multiple methods at the same time before queueing.
    /// </summary>
    public int MaximumParallelInvocationsPerClient
    {
        get => _maximumParallelInvocationsPerClient;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

            _maximumParallelInvocationsPerClient = value;
        }
    }

    /// <summary>
    /// When <see langword="false"/>, <see cref="IServiceProviderIsService"/> determines if a Hub method parameter will be injected from the DI container.
    /// Parameters can be explicitly marked with an attribute that implements <see cref="IFromServiceMetadata"/> with or without this option set.
    /// </summary>
    /// <remarks>
    /// False by default. Hub method arguments will be resolved from a DI container if possible.
    /// </remarks>
    public bool DisableImplicitFromServicesParameters { get; set; }

    /// <summary>
    /// Gets or sets the maximum bytes to buffer per connection when using stateful reconnect.
    /// </summary>
    /// <remarks>Defaults to 100,000 bytes.</remarks>
    public long StatefulReconnectBufferSize { get; set; } = 100_000;
}
