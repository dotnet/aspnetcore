// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

internal sealed class CascadingQueryValueProvider : IComponent, ICascadingValueSupplier, IDisposable
{
    private readonly Dictionary<ReadOnlyMemory<char>, StringSegmentAccumulator> _queryParameterValuesByName = new(QueryParameterNameComparer.Instance);

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
            UpdateAllSubscribers();

            Navigation.LocationChanged += HandleLocationChanged;
        }

        parameters.SetParameterProperties(this);

        _handle.Render(Render);

        return Task.CompletedTask;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateQueryParameterValues();
        UpdateAllSubscribers();
    }

    private void UpdateQueryParameterValues()
    {
        _queryParameterValuesByName.Clear();

        var url = Navigation.Uri;
        var queryStartPos = url.IndexOf('?');

        if (queryStartPos < 0)
        {
            return;
        }

        var queryEndPos = url.IndexOf('#', queryStartPos);
        var query = url.AsMemory(queryStartPos..(queryEndPos < 0 ? url.Length : queryEndPos));
        var queryStringEnumerable = new QueryStringEnumerable(query);

        foreach (var suppliedPair in queryStringEnumerable)
        {
            var decodedName = suppliedPair.DecodeName();
            var decodedValue = suppliedPair.DecodeValue();

            // This is safe because we don't mutate the dictionary while the ref local is in scope.
            ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(_queryParameterValuesByName, decodedName, out _);
            values.Add(decodedValue);
        }
    }

    private void UpdateAllSubscribers()
    {
        if (_subscribers is null)
        {
            return;
        }

        foreach (var subscriber in _subscribers)
        {
            subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
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
        var expectedValueType = parameterInfo.PropertyType;
        var isArray = expectedValueType.IsArray;
        var elementType = isArray ? expectedValueType.GetElementType()! : expectedValueType;

        if (!UrlValueConstraint.TryGetByTargetType(elementType, out var parser))
        {
            throw new InvalidOperationException($"Querystring values cannot be parsed as type '{elementType}'.");
        }

        var valueName = parameterInfo.Attribute.Name ?? parameterInfo.PropertyName;
        var values = _queryParameterValuesByName.GetValueOrDefault(valueName.AsMemory());

        if (isArray)
        {
            return parser.ParseMultiple(values, valueName);
        }

        if (values.Count > 0)
        {
            return parser.Parse(values[0].Span, valueName);
        }

        return default;
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
