// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Routing;

internal abstract class ActionEndpointDataSourceBase : EndpointDataSource, IDisposable
{
    private readonly IActionDescriptorCollectionProvider _actions;

    // The following are protected by this lock for WRITES only. This pattern is similar
    // to DefaultActionDescriptorChangeProvider - see comments there for details on
    // all of the threading behaviors.
    protected readonly object Lock = new object();

    // Protected for READS and WRITES.
    protected readonly List<Action<EndpointBuilder>> Conventions;
    protected readonly List<Action<EndpointBuilder>> FinallyConventions;

    private List<Endpoint>? _endpoints;
    private CancellationTokenSource? _cancellationTokenSource;
    private IChangeToken? _changeToken;
    private IDisposable? _disposable;

    public ActionEndpointDataSourceBase(IActionDescriptorCollectionProvider actions)
    {
        _actions = actions;

        Conventions = new List<Action<EndpointBuilder>>();
        FinallyConventions = new List<Action<EndpointBuilder>>();
    }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _endpoints;
        }
    }

    public override IReadOnlyList<Endpoint> GetGroupedEndpoints(RouteGroupContext context)
    {
        return CreateEndpoints(
            context.Prefix,
            _actions.ActionDescriptors.Items,
            Conventions,
            context.Conventions,
            FinallyConventions,
            context.FinallyConventions);
    }

    // Will be called with the lock.
    protected abstract List<Endpoint> CreateEndpoints(
        RoutePattern? groupPrefix,
        IReadOnlyList<ActionDescriptor> actions,
        IReadOnlyList<Action<EndpointBuilder>> conventions,
        IReadOnlyList<Action<EndpointBuilder>> groupConventions,
        IReadOnlyList<Action<EndpointBuilder>> finallyConventions,
        IReadOnlyList<Action<EndpointBuilder>> groupFinallyConventions);

    protected void Subscribe()
    {
        // IMPORTANT: this needs to be called by the derived class to avoid the fragile base class
        // problem. We can't call this in the base-class constuctor because it's too early.
        //
        // It's possible for someone to override the collection provider without providing
        // change notifications. If that's the case we won't process changes.
        if (_actions is ActionDescriptorCollectionProvider collectionProviderWithChangeToken)
        {
            _disposable = ChangeToken.OnChange(
                collectionProviderWithChangeToken.GetChangeToken,
                UpdateEndpoints);
        }
    }

    public override IChangeToken GetChangeToken()
    {
        Initialize();
        Debug.Assert(_changeToken != null);
        Debug.Assert(_endpoints != null);
        return _changeToken;
    }

    public void Dispose()
    {
        // Once disposed we won't process updates anymore, but we still allow access to the endpoints.
        _disposable?.Dispose();
        _disposable = null;
    }

    private void Initialize()
    {
        if (_endpoints == null)
        {
            lock (Lock)
            {
                if (_endpoints == null)
                {
                    UpdateEndpoints();
                }
            }
        }
    }

    private void UpdateEndpoints()
    {
        lock (Lock)
        {
            var endpoints = CreateEndpoints(
                groupPrefix: null,
                _actions.ActionDescriptors.Items,
                conventions: Conventions,
                groupConventions: Array.Empty<Action<EndpointBuilder>>(),
                finallyConventions: FinallyConventions,
                groupFinallyConventions: Array.Empty<Action<EndpointBuilder>>());
            // Step 1 - capture old token
            var oldCancellationTokenSource = _cancellationTokenSource;

            // Step 2 - update endpoints
            _endpoints = endpoints;

            // Step 3 - create new change token
            _cancellationTokenSource = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);

            // Step 4 - trigger old token
            oldCancellationTokenSource?.Cancel();
        }
    }
}
