// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

internal sealed class CascadingQueryValueProvider : IComponent, ICascadingValueSupplierFactory, IDisposable
{
    private readonly Dictionary<SupplierKey, QueryValueSupplier> _cachedSuppliers = new();
    private readonly Dictionary<ReadOnlyMemory<char>, StringSegmentAccumulator> _queryParameterValuesByName = new(QueryParameterNameComparer.Instance);

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
            UpdateAllSuppliers();

            Navigation.LocationChanged += HandleLocationChanged;
        }

        parameters.SetParameterProperties(this);

        _handle.Render(Render);

        return Task.CompletedTask;
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateQueryParameterValues();
        UpdateAllSuppliers();
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

    private void UpdateAllSuppliers()
    {
        foreach (var supplier in _cachedSuppliers.Values)
        {
            UpdateSupplier(supplier);
        }
    }

    private void UpdateSupplier(QueryValueSupplier supplier)
    {
        // This is safe because we don't mutate the dictionary while the ref local is in scope.
        ref var existingValues = ref CollectionsMarshal.GetValueRefOrNullRef(_queryParameterValuesByName, supplier.ValueName);

        if (!Unsafe.IsNullRef(ref existingValues))
        {
            supplier.UpdateValues(ref existingValues);
        }
        else
        {
            var emptyValues = default(StringSegmentAccumulator);
            supplier.UpdateValues(ref emptyValues);
        }
    }

    private void Render(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }

    bool ICascadingValueSupplierFactory.TryGetValueSupplier(object attribute, Type parameterType, string parameterName, [NotNullWhen(true)] out ICascadingValueSupplier? result)
    {
        result = default;

        if (attribute is not SupplyParameterFromQueryAttribute supplyFromQueryAttribute)
        {
            return false;
        }

        var parameterSpecifiedName = supplyFromQueryAttribute.Name;
        var valueName = string.IsNullOrEmpty(parameterSpecifiedName) ? parameterName : parameterSpecifiedName;
        var key = new SupplierKey(parameterType, valueName.AsMemory());

        if (!_cachedSuppliers.TryGetValue(key, out var supplier))
        {
            supplier = QueryValueSupplier.Create(parameterType, valueName);

            // Supply an initial value if possible.
            UpdateSupplier(supplier);

            _cachedSuppliers.Add(key, supplier);
        }

        result = supplier;
        return true;
    }

    void IDisposable.Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
    }

    private readonly struct SupplierKey(Type targetType, ReadOnlyMemory<char> valueName) : IEquatable<SupplierKey>
    {
        private readonly Type _targetType = targetType;
        private readonly ReadOnlyMemory<char> _valueName = valueName;

        public bool Equals(SupplierKey other)
            => _targetType.Equals(other._targetType) && QueryParameterNameComparer.Instance.Equals(_valueName, other._valueName);

        public override bool Equals(object? obj)
            => obj is SupplierKey other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(_targetType, QueryParameterNameComparer.Instance.GetHashCode(_valueName));
    }

    private sealed class QueryValueSupplier : ICascadingValueSupplier
    {
        private readonly UrlValueConstraint _parser;
        private readonly bool _isArray;
        private readonly string _valueName;

        private HashSet<ComponentState>? _subscribers;
        private object? _currentValue;

        public ReadOnlyMemory<char> ValueName => _valueName.AsMemory();

        public static QueryValueSupplier Create(Type targetType, string valueName)
        {
            var isArray = targetType.IsArray;
            var elementType = isArray ? targetType.GetElementType()! : targetType;

            if (!UrlValueConstraint.TryGetByTargetType(elementType, out var parser))
            {
                throw new NotSupportedException($"Querystring values cannot be parsed as type '{elementType}'.");
            }

            return new(isArray, valueName, parser);
        }

        private QueryValueSupplier(bool isArray, string valueName, UrlValueConstraint parser)
        {
            _isArray = isArray;
            _valueName = valueName;
            _parser = parser;
        }

        public void UpdateValues(ref StringSegmentAccumulator values)
        {
            var oldValue = _currentValue;
            _currentValue = _isArray
                ? _parser.ParseMultiple(values, _valueName)
                : values.Count == 0
                    ? default
                    : _parser.Parse(values[0].Span, _valueName);

            if (_subscribers is null || !ChangeDetection.MayHaveChanged(oldValue, _currentValue))
            {
                return;
            }

            foreach (var subscriber in _subscribers)
            {
                subscriber.NotifyCascadingValueChanged(ParameterViewLifetime.Unbound);
            }
        }

        object? ICascadingValueSupplier.CurrentValue => _currentValue;

        bool ICascadingValueSupplier.CurrentValueIsFixed => false;

        void ICascadingValueSupplier.Subscribe(ComponentState subscriber)
        {
            _subscribers ??= new();
            _subscribers.Add(subscriber);
        }

        void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber)
        {
            _subscribers?.Remove(subscriber);
        }
    }
}
