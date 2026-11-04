// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// The default implementation for the <see cref="ConnectionContext"/>.
/// </summary>
public class DefaultConnectionContext : ConnectionContext,
                                        IConnectionIdFeature,
                                        IConnectionItemsFeature,
                                        IConnectionTransportFeature,
                                        IConnectionUserFeature,
                                        IConnectionLifetimeFeature,
                                        IConnectionEndPointFeature
{
    private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();

    /// <summary>
    /// Creates the <see cref="DefaultConnectionContext"/> without Pipes to avoid upfront allocations.
    /// The caller is expected to set the <see cref="Transport"/> and <see cref="Application"/> pipes manually.
    /// </summary>
    public DefaultConnectionContext() :
        this(Guid.NewGuid().ToString())
    {
    }

    /// <summary>
    /// Creates the <see cref="DefaultConnectionContext"/> without Pipes to avoid upfront allocations.
    /// The caller is expected to set the <see cref="Transport"/> and <see cref="Application"/> pipes manually.
    /// </summary>
    /// <param name="id">The <see cref="ConnectionId"/>.</param>
    public DefaultConnectionContext(string id)
    {
        ConnectionId = id;

        Features = new FeatureCollection();
        Features.Set<IConnectionUserFeature>(this);
        Features.Set<IConnectionItemsFeature>(this);
        Features.Set<IConnectionIdFeature>(this);
        Features.Set<IConnectionTransportFeature>(this);
        Features.Set<IConnectionLifetimeFeature>(this);
        Features.Set<IConnectionEndPointFeature>(this);

        ConnectionClosed = _connectionClosedTokenSource.Token;
    }

    /// <summary>
    /// Creates the DefaultConnectionContext with the given <paramref name="transport"/> and <paramref name="application"/> pipes.
    /// </summary>
    /// <param name="id">The <see cref="ConnectionId"/>.</param>
    /// <param name="transport">The <see cref="Transport"/>.</param>
    /// <param name="application">The <see cref="Application"/>.</param>
    public DefaultConnectionContext(string id, IDuplexPipe transport, IDuplexPipe application)
        : this(id)
    {
        Transport = transport;
        Application = application;
    }

    /// <inheritdoc />
    public override string ConnectionId { get; set; }

    /// <inheritdoc />
    public override IFeatureCollection Features { get; }

    /// <inheritdoc />
    public ClaimsPrincipal? User { get; set; }

    /// <inheritdoc />
    public override IDictionary<object, object?> Items { get; set; } = new ConnectionItems();

    /// <inheritdoc />
    public IDuplexPipe? Application { get; set; }

    /// <inheritdoc />
    public override IDuplexPipe Transport { get; set; } = default!;

    /// <inheritdoc />
    public override CancellationToken ConnectionClosed { get; set; }

    /// <inheritdoc />
    public override EndPoint? LocalEndPoint { get; set; }

    /// <inheritdoc />
    public override EndPoint? RemoteEndPoint { get; set; }

    /// <inheritdoc />
    public override void Abort(ConnectionAbortedException abortReason)
    {
        ThreadPool.UnsafeQueueUserWorkItem(cts => ((CancellationTokenSource)cts!).Cancel(), _connectionClosedTokenSource);
    }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        _connectionClosedTokenSource.Dispose();
        return base.DisposeAsync();
    }
}
