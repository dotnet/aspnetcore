// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// An <see cref="EndpointDataSource"/> for static assets.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class StaticAssetsEndpointDataSource : EndpointDataSource
{
    private readonly object _lock = new();
    private readonly StaticAssetsManifest _manifest;
    private readonly string _manifestName;
    private readonly StaticAssetEndpointFactory _endpointFactory;
    private readonly List<Action<EndpointBuilder>> _conventions = [];
    private readonly List<Action<EndpointBuilder>> _finallyConventions = [];
    private List<Endpoint> _endpoints = null!;
    private CancellationTokenSource _cancellationTokenSource;
    private CancellationChangeToken _changeToken;

    internal StaticAssetsEndpointDataSource(StaticAssetsManifest manifest, StaticAssetEndpointFactory endpointFactory, string manifestName, List<StaticAssetDescriptor> descriptors)
    {
        _manifest = manifest;
        _manifestName = manifestName;
        _endpointFactory = endpointFactory;
        _cancellationTokenSource = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);

        DefaultBuilder = new StaticAssetsEndpointConventionBuilder(
            _lock,
            descriptors,
            _conventions,
            _finallyConventions);
    }

    /// <summary>
    /// Gets the manifest name associated with this static asset endpoint data source.
    /// </summary>
    public string ManifestName => _manifestName;

    /// <inheritdoc />
    public StaticAssetsEndpointConventionBuilder DefaultBuilder { get; internal set; }

    /// <inheritdoc />
    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            // Note it is important that this is lazy, since we only want to create the endpoints after the user had a chance to populate
            // the list of conventions.
            // The order is as follows:
            // * MapRazorComponents gets called and the data source gets created.
            // * The RazorComponentEndpointConventionBuilder is returned and the user gets a chance to call on it to add conventions.
            // * The first request arrives and the DfaMatcherBuilder accesses the data sources to get the endpoints.
            // * The endpoints get created and the conventions get applied.
            Initialize();
            Debug.Assert(_changeToken != null);
            Debug.Assert(_endpoints != null);
            return _endpoints;
        }
    }

    private void Initialize()
    {
        if (_endpoints == null)
        {
            lock (_lock)
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
        lock (_lock)
        {
            var endpoints = new List<Endpoint>();

            foreach (var resource in _manifest.Endpoints)
            {
                endpoints.Add(_endpointFactory.Create(resource, _conventions, _finallyConventions));
            }

            var oldCancellationTokenSource = _cancellationTokenSource;
            _endpoints = endpoints;
            _cancellationTokenSource = new CancellationTokenSource();
            _changeToken = new CancellationChangeToken(_cancellationTokenSource.Token);
            oldCancellationTokenSource?.Cancel();
            oldCancellationTokenSource?.Dispose();
        }
    }

    /// <inheritdoc />
    public override IChangeToken GetChangeToken()
    {
        Initialize();
        Debug.Assert(_changeToken != null);
        Debug.Assert(_endpoints != null);
        return _changeToken;
    }

    private string GetDebuggerDisplay() => _manifestName;
}
