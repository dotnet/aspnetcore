// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents an <see cref="EndpointDataSource"/> whose values come from a collection of <see cref="EndpointDataSource"/> instances.
/// </summary>
[DebuggerDisplay("{DebuggerDisplayString,nq}")]
public sealed class CompositeEndpointDataSource : EndpointDataSource, IDisposable
{
    private readonly object _lock = new();
    private readonly ICollection<EndpointDataSource> _dataSources;

    private IReadOnlyList<Endpoint>? _endpoints;
    private IChangeToken? _consumerChangeToken;
    private CancellationTokenSource? _cts;
    private List<IDisposable>? _changeTokenRegistrations;
    private bool _disposed;

    internal CompositeEndpointDataSource(ObservableCollection<EndpointDataSource> dataSources)
    {
        dataSources.CollectionChanged += OnDataSourcesChanged;
        _dataSources = dataSources;
    }

    /// <summary>
    /// Instantiates a <see cref="CompositeEndpointDataSource"/> object from <paramref name="endpointDataSources"/>.
    /// </summary>
    /// <param name="endpointDataSources">An collection of <see cref="EndpointDataSource" /> objects.</param>
    /// <returns>A <see cref="CompositeEndpointDataSource"/>.</returns>
    public CompositeEndpointDataSource(IEnumerable<EndpointDataSource> endpointDataSources)
    {
        _dataSources = new List<EndpointDataSource>();

        foreach (var dataSource in endpointDataSources)
        {
            _dataSources.Add(dataSource);
        }
    }

    private void OnDataSourcesChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleChange(collectionChanged: true);

    /// <summary>
    /// Returns the collection of <see cref="EndpointDataSource"/> instances associated with the object.
    /// </summary>
    public IEnumerable<EndpointDataSource> DataSources => _dataSources;

    /// <summary>
    /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Endpoint"/> instances.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public override IChangeToken GetChangeToken()
    {
        EnsureChangeTokenInitialized();
        return _consumerChangeToken;
    }

    /// <summary>
    /// Returns a read-only collection of <see cref="Endpoint"/> instances.
    /// </summary>
    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            EnsureEndpointsInitialized();
            return _endpoints;
        }
    }

    /// <inheritdoc/>
    public override IReadOnlyList<RouteEndpoint> GetGroupedEndpoints(RouteGroupContext context)
    {
        if (_dataSources.Count is 0)
        {
            return Array.Empty<RouteEndpoint>();
        }

        // We could try to optimize the single data source case by returning its result directly like GroupDataSource does,
        // but the CompositeEndpointDataSourceTest class was picky about the Endpoints property creating a shallow copy,
        // so we'll shallow copy here for consistency.
        var groupedEndpoints = new List<RouteEndpoint>();

        foreach (var dataSource in _dataSources)
        {
            groupedEndpoints.AddRange(dataSource.GetGroupedEndpoints(context));
        }

        // There's no need to cache these the way we do with _endpoints. This is only ever used to get intermediate results.
        // Anything using the DataSourceDependentCache like the DfaMatcher will resolve the cached Endpoints property.
        return groupedEndpoints;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // CompositeDataSource is registered as a singleton by default by AddRouting().
        // UseEndpoints() adds all root data sources to this singleton.
        lock (_lock)
        {
            _disposed = true;

            if (_changeTokenRegistrations is not null)
            {
                foreach (var registration in _changeTokenRegistrations)
                {
                    registration.Dispose();
                }
            }

            if (_dataSources is ObservableCollection<EndpointDataSource> observableDataSources)
            {
                observableDataSources.CollectionChanged -= OnDataSourcesChanged;
            }

            foreach (var dataSource in _dataSources)
            {
                (dataSource as IDisposable)?.Dispose();
            }
        }
    }

    // Defer initialization to avoid doing lots of reflection on startup.
    [MemberNotNull(nameof(_endpoints))]
    private void EnsureEndpointsInitialized()
    {
        lock (_lock)
        {
            if (_endpoints is null)
            {
                // Now that we're caching the _enpoints, we're responsible for keeping them up-to-date even if the caller
                // hasn't started listening for changes themselves yet.
                EnsureChangeTokenInitialized();

                // Note: we can't use DataSourceDependentCache here because we also need to handle a list of change
                // tokens, which is a complication most of our code doesn't have.
                _endpoints = _dataSources.SelectMany(d => d.Endpoints).ToArray();
            }
        }
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        lock (_lock)
        {
            if (_consumerChangeToken is null)
            {
                // This is our first time initializing the change token, so the collection has "changed" from nothing.
                CreateChangeTokenUnsynchronized(collectionChanged: true);
            }
        }
    }

    private void HandleChange(bool collectionChanged)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            // Don't update endpoints if no one has read them yet.
            if (_endpoints is not null)
            {
                // Refresh the endpoints from datasource so that callbacks can get the latest endpoints
                _endpoints = _dataSources.SelectMany(d => d.Endpoints).ToArray();
            }

            // Prevent consumers from re-registering callback to inflight events as that can
            // cause a stackoverflow
            // Example:
            // 1. B registers A
            // 2. A fires event causing B's callback to get called
            // 3. B executes some code in its callback, but needs to re-register callback
            //    in the same callback
            var oldTokenSource = _cts;

            // Don't create a new change token if no one is listening.
            if (oldTokenSource is not null)
            {
                CreateChangeTokenUnsynchronized(collectionChanged);
                // Raise consumer callbacks. Any new callback registration would happen on the new token
                // created in earlier step.
                oldTokenSource.Cancel();
            }
        }
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void CreateChangeTokenUnsynchronized(bool collectionChanged)
    {
        _cts = new CancellationTokenSource();
        _consumerChangeToken = new CancellationChangeToken(_cts.Token);

        if (collectionChanged)
        {
            if (_changeTokenRegistrations is null)
            {
                _changeTokenRegistrations = new();
            }
            else
            {
                foreach (var registration in _changeTokenRegistrations)
                {
                    registration.Dispose();
                }
                _changeTokenRegistrations.Clear();
            }

            foreach (var dataSource in _dataSources)
            {
                _changeTokenRegistrations.Add(ChangeToken.OnChange(
                    dataSource.GetChangeToken,
                    () => HandleChange(collectionChanged: false)));
            }
        }
    }

    // Use private variable '_endpoints' to avoid initialization
    private string DebuggerDisplayString => GetDebuggerDisplayStringForEndpoints(_endpoints);
}
