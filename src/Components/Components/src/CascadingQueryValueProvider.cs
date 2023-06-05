// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

internal sealed class CascadingQueryValueProvider : IComponent, ICascadingValueSupplier, IDisposable
{
    private readonly QueryParameterValueSupplier _queryParameterValueSupplier = new();

    private HashSet<ComponentState>? _subscribers;
    private RenderHandle _handle;
    private bool _hasSetInitialParameters;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (!_hasSetInitialParameters)
        {
            _hasSetInitialParameters = true;

            UpdateQueryParameterValues();

            Navigation.LocationChanged += HandleLocationChanged;
        }

        parameters.SetParameterProperties(this);

        _handle.Render(Render);

        return Task.CompletedTask;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        => UpdateQueryParameterValues();

    private void UpdateQueryParameterValues()
    {
        var query = GetQueryString(Navigation.Uri);

        _queryParameterValueSupplier.ReadParametersFromQuery(query);

        if (_subscribers is null)
        {
            return;
        }

        foreach (var subscriber in _subscribers)
        {
            subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
        }

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

    private void Render(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => parameterInfo.Attribute is SupplyParameterFromQueryAttribute;

    bool ICascadingValueSupplier.CurrentValueIsFixed => false;

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        var queryParameterName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        return _queryParameterValueSupplier.GetQueryParameterValue(parameterInfo.PropertyType, queryParameterName);
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber)
    {
        _subscribers ??= new();
        _subscribers.Add(subscriber);
    }

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber)
    {
        _subscribers?.Remove(subscriber);
    }

    void IDisposable.Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}
