// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    private List<Endpoint>? _endpoints;
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
    public override IReadOnlyList<Endpoint> GetGroupedEndpoints(RouteGroupContext context)
    {
        if (_dataSources.Count is 0)
        {
            return Array.Empty<Endpoint>();
        }

        // We could try to optimize the single data source case by returning its result directly like GroupDataSource does,
        // but the CompositeEndpointDataSourceTest class was picky about the Endpoints property creating a shallow copy,
        // so we'll shallow copy here for consistency.
        var groupedEndpoints = new List<Endpoint>();

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
        List<IDisposable>? disposables = null;

        lock (_lock)
        {
            _disposed = true;

            if (_dataSources is ObservableCollection<EndpointDataSource> observableDataSources)
            {
                observableDataSources.CollectionChanged -= OnDataSourcesChanged;
            }

            foreach (var dataSource in _dataSources)
            {
                if (dataSource is IDisposable disposableDataSource)
                {
                    disposables ??= new List<IDisposable>();
                    disposables.Add(disposableDataSource);
                }
            }

            if (_changeTokenRegistrations is { Count: > 0 })
            {
                disposables ??= new List<IDisposable>();
                disposables.AddRange(_changeTokenRegistrations);
            }
        }

        // Dispose everything outside of the lock in case a registration is blocking on HandleChange completing
        // on another thread or something.
        if (disposables is not null)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }

    // Defer initialization to avoid doing lots of reflection on startup.
    [MemberNotNull(nameof(_endpoints))]
    private void EnsureEndpointsInitialized()
    {
        if (_endpoints is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_endpoints is not null)
            {
                return;
            }

            // Now that we're caching the _enpoints, we're responsible for keeping them up-to-date even if the caller
            // hasn't started listening for changes themselves yet.
            EnsureChangeTokenInitialized();

            // Note: we can't use DataSourceDependentCache here because we also need to handle a list of change
            // tokens, which is a complication most of our code doesn't have.
            CreateEndpointsUnsynchronized();
        }
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void EnsureChangeTokenInitialized()
    {
        if (_consumerChangeToken is not null)
        {
            return;
        }

        lock (_lock)
        {
            if (_consumerChangeToken is not null)
            {
                return;
            }

            // This is our first time initializing the change token, so the collection has "changed" from nothing.
            CreateChangeTokenUnsynchronized(collectionChanged: true);
        }
    }

    private void HandleChange(bool collectionChanged)
    {
        CancellationTokenSource? oldTokenSource = null;
        List<IDisposable>? oldChangeTokenRegistrations = null;

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            // Prevent consumers from re-registering callback to in-flight events as that can
            // cause a stack overflow.
            // Example:
            // 1. B registers A.
            // 2. A fires event causing B's callback to get called.
            // 3. B executes some code in its callback, but needs to re-register callback
            //    in the same callback.
            oldTokenSource = _cts;
            oldChangeTokenRegistrations = _changeTokenRegistrations;

            // Don't create a new change token if no one is listening.
            if (oldTokenSource is not null)
            {
                // We have to hook to any OnChange callbacks before caching endpoints,
                // otherwise we might miss changes that occurred to one of the _dataSources after caching.
                CreateChangeTokenUnsynchronized(collectionChanged);
            }

            // Don't update endpoints if no one has read them yet.
            if (_endpoints is not null)
            {
                // Refresh the endpoints from data source so that callbacks can get the latest endpoints.
                CreateEndpointsUnsynchronized();
            }
        }

        // Disposing registrations can block on user defined code on running on other threads that could try to acquire the _lock.
        if (collectionChanged && oldChangeTokenRegistrations is not null)
        {
            foreach (var registration in oldChangeTokenRegistrations)
            {
                registration.Dispose();
            }
        }

        // Raise consumer callbacks. Any new callback registration would happen on the new token created in earlier step.
        // Avoid raising callbacks inside a lock.
        oldTokenSource?.Cancel();
    }

    [MemberNotNull(nameof(_consumerChangeToken))]
    private void CreateChangeTokenUnsynchronized(bool collectionChanged)
    {
        var cts = new CancellationTokenSource();

        if (collectionChanged)
        {
            _changeTokenRegistrations = new();
            foreach (var dataSource in _dataSources)
            {
                _changeTokenRegistrations.Add(ChangeToken.OnChange(
                    dataSource.GetChangeToken,
                    () => HandleChange(collectionChanged: false)));
            }
        }

        _cts = cts;
        _consumerChangeToken = new CancellationChangeToken(cts.Token);
    }

    [MemberNotNull(nameof(_endpoints))]
    private void CreateEndpointsUnsynchronized()
    {
        var endpoints = new List<Endpoint>();

        foreach (var dataSource in _dataSources)
        {
            endpoints.AddRange(dataSource.Endpoints);
        }

        // Only cache _endpoints after everything succeeds without throwing.
        // We don't want to create a negative cache which would cause 404s when there should be 500s.
        _endpoints = endpoints;
    }

    // Use private variable '_endpoints' to avoid initialization
    private string DebuggerDisplayString => GetDebuggerDisplayStringForEndpoints(_endpoints);
}
