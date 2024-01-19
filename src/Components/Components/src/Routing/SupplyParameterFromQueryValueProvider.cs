// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Routing;

internal sealed class SupplyParameterFromQueryValueProvider(NavigationManager navigationManager) : ICascadingValueSupplier, IDisposable
{
    private QueryParameterValueSupplier? _queryParameterValueSupplier;

    private HashSet<ComponentState>? _subscribers;
    private HashSet<ComponentState>? _pendingSubscribers;

    private string? _lastUri;
    private bool _isSubscribedToLocationChanges;
    private bool _queryChanged;

    public bool IsFixed => false;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromQueryAttribute;

    public object? GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        TryUpdateUri();

        var attribute = (SupplyParameterFromQueryAttribute)parameterInfo.Attribute; // Must be a valid cast because we check in CanSupplyValue
        var queryParameterName = attribute.Name ?? parameterInfo.PropertyName;
        return _queryParameterValueSupplier.GetQueryParameterValue(parameterInfo.PropertyType, queryParameterName);
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        if (_pendingSubscribers?.Count > 0 || (TryUpdateUri() && _isSubscribedToLocationChanges))
        {
            // Renderer.RenderInExistingBatch triggers Unsubscribe via ProcessDisposalQueueInExistingBatch after subscribing with any new components,
            // so this branch should be taken iff there's a pending OnLocationChanged event for the current Uri that we're already subscribed to.
            _pendingSubscribers ??= new();
            _pendingSubscribers.Add(subscriber);
            return;
        }

        _subscribers ??= new();
        _subscribers.Add(subscriber);
        SubscribeToLocationChanges();
    }

    public void Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        // ICascadingValueSupplier is internal, and Subscribe should always precede Unsubscribe.
        Debug.Assert(_subscribers is not null);
        _subscribers.Remove(subscriber);
        _pendingSubscribers?.Remove(subscriber);

        if (_subscribers.Count == 0 && _pendingSubscribers is null or { Count: 0 })
        {
            UnsubscribeFromLocationChanges();
        }
    }

    [MemberNotNull(nameof(_queryParameterValueSupplier))]
    private bool TryUpdateUri()
    {
        _queryParameterValueSupplier ??= new();

        // NavigationManager triggers Router.OnLocationChanged which calls GetCurrentValue before this class's OnLocationHandler
        // gets a chance to run, so we have to compare strings rather than rely on OnLocationChanged always running before Uri updates.
        if (navigationManager.Uri == _lastUri)
        {
            return false;
        }

        var query = GetQueryString(navigationManager.Uri);

        if (!query.Span.SequenceEqual(GetQueryString(_lastUri).Span))
        {
           _queryChanged = true;
           _queryParameterValueSupplier.ReadParametersFromQuery(query);
        }

        _lastUri = navigationManager.Uri;
        return true;

        static ReadOnlyMemory<char> GetQueryString(string? url)
        {
            var queryStartPos = url?.IndexOf('?') ?? -1;

            if (queryStartPos < 0)
            {
                return default;
            }

            Debug.Assert(url is not null);
            var queryEndPos = url.IndexOf('#', queryStartPos);
            return url.AsMemory(queryStartPos..(queryEndPos < 0 ? url.Length : queryEndPos));
        }
    }

    private void SubscribeToLocationChanges()
    {
        if (_isSubscribedToLocationChanges)
        {
            return;
        }

        _isSubscribedToLocationChanges = true;
        navigationManager.LocationChanged += OnLocationChanged;
    }

    private void UnsubscribeFromLocationChanges()
    {
        if (!_isSubscribedToLocationChanges)
        {
            return;
        }

        _isSubscribedToLocationChanges = false;
        navigationManager.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        Debug.Assert(_subscribers is not null);

        TryUpdateUri();

        if (_queryChanged)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
            }

            _queryChanged = false;
        }

        if (_pendingSubscribers is not null)
        {
            foreach (var subscriber in _pendingSubscribers)
            {
                _subscribers.Add(subscriber);
            }

            _pendingSubscribers.Clear();
        }
    }

    public void Dispose() => UnsubscribeFromLocationChanges();
}
