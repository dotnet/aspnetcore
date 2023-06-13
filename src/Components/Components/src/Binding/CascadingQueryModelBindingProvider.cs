// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components.Binding;

/// <summary>
/// Enables component parameters to be supplied from the query string with <see cref="SupplyParameterFromQueryAttribute"/>.
/// </summary>
public sealed class CascadingQueryModelBindingProvider : CascadingModelBindingProvider, IDisposable
{
    private readonly QueryParameterValueSupplier _queryParameterValueSupplier = new();
    private readonly NavigationManager _navigationManager;

    private HashSet<ComponentState>? _subscribers;
    private bool _haveQueryParametersChanged = true;

    /// <inheritdoc/>
    protected internal override bool AreValuesFixed => false;

    /// <summary>
    /// Constructs a new instance of <see cref="CascadingQueryModelBindingProvider"/>.
    /// </summary>
    public CascadingQueryModelBindingProvider(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc/>
    protected internal override bool SupportsCascadingParameterAttributeType(Type attributeType)
        => attributeType == typeof(SupplyParameterFromQueryAttribute);

    /// <inheritdoc/>
    protected internal override bool SupportsParameterType(Type type)
        => QueryParameterValueSupplier.CanSupplyValueForType(type);

    /// <inheritdoc/>
    protected internal override bool CanSupplyValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
        // We can always supply a value; it'll just be null if there's no match.
        => true;

    /// <inheritdoc/>
    protected internal override object? GetCurrentValue(ModelBindingContext? bindingContext, in CascadingParameterInfo parameterInfo)
    {
        if (_haveQueryParametersChanged)
        {
            _haveQueryParametersChanged = false;
            UpdateQueryParameters();
        }

        var queryParameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        return _queryParameterValueSupplier.GetQueryParameterValue(parameterInfo.PropertyType, queryParameterName);
    }

    /// <inheritdoc/>
    protected internal override void Subscribe(ComponentState subscriber)
    {
        _subscribers ??= new();
        _subscribers.Add(subscriber);
    }

    /// <inheritdoc/>
    protected internal override void Unsubscribe(ComponentState subscriber)
    {
        _subscribers?.Remove(subscriber);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _haveQueryParametersChanged = true;

        if (_subscribers is not null)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
            }
        }
    }

    private void UpdateQueryParameters()
    {
        var query = GetQueryString(_navigationManager.Uri);

        _queryParameterValueSupplier.ReadParametersFromQuery(query);

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

    void IDisposable.Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
