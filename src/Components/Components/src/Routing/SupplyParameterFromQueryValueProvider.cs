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
    private bool _isSubscribedToLocationChanges;
    private string? _lastUri;

    public bool IsFixed => false;

    public bool CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromQueryAttribute;

    public object? GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        TryUpdateQueryParameters();

        var attribute = (SupplyParameterFromQueryAttribute)parameterInfo.Attribute; // Must be a valid cast because we check in CanSupplyValue
        var queryParameterName = attribute.Name ?? parameterInfo.PropertyName;
        return _queryParameterValueSupplier.GetQueryParameterValue(parameterInfo.PropertyType, queryParameterName);
    }

    public void Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
    {
        if (_pendingSubscribers?.Count > 0 || (TryUpdateQueryParameters() && _isSubscribedToLocationChanges))
        {
            // This branch is only taken if there's a pending OnLocationChanged event for the current Uri that we're already subscribed to.
            // We'll add the _pendingSubscribers to _subscribers and clear _pendingSubscribers during the upcoming call to OnLocationChanged.
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

        if (_subscribers.Count == 0 && !(_pendingSubscribers?.Count > 0))
        {
            UnsubscribeFromLocationChanges();
        }
    }

    [MemberNotNull(nameof(_queryParameterValueSupplier))]
    private bool TryUpdateQueryParameters()
    {
        _queryParameterValueSupplier ??= new();

        // Router.OnLocationChanged calls GetCurrentValue before our own OnLocationChanged handler,
        // so we have to compare strings rather than rely on a bool set in OnLocationChanged.
        if (navigationManager.Uri == _lastUri)
        {
            return false;
        }

        var query = GetQueryString(navigationManager.Uri);
        _queryParameterValueSupplier.ReadParametersFromQuery(query);
        _lastUri = navigationManager.Uri;
        return true;

        static ReadOnlyMemory<char> GetQueryString(string url)
        {
            var queryStartPos = url.IndexOf('?');

            if (queryStartPos < 0)
            {
                return default;
            }

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
        foreach (var subscriber in _subscribers)
        {
            subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
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
