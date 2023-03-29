// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Components;

public class CascadingModelBinder : IComponent, ICascadingValueComponent
{
    private RenderHandle _handler;
    private bool _hasRendered;
    public BindingContext? _bindingContext;

    public object? CurrentValue => null;

    public bool CurrentValueIsFixed => true;

    [Parameter] public RenderFragment ChildContent { get; set; }

    [Inject] public NavigationManager Navigation { get; set; } = null;

    [Inject] public IFormStateProvider FormStateProvider { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        _handler = renderHandle;
    }

    ICascadingValueComponent ICascadingValueComponent.GetSupplier(Type valueType, string? valueName, string? source)
    {
        if (source == "Query")
        {
            return new QueryParameterSupplier(Navigation, valueName);
        }
        if (source == "Form" && FormStateProvider.IsAvailable)
        {
            return new FormParameterSupplier(valueType, valueName, FormStateProvider, _bindingContext);
        }

        throw new InvalidOperationException();
    }

    public bool CanSupplyValue(Type valueType, string? valueName, string? source)
    {
        return (Type.GetTypeCode(valueType) == TypeCode.String && source == "Query") || (source == "Form" && FormStateProvider.IsAvailable);
    }

    private class FormParameterSupplier : ICascadingValueComponent
    {
        private readonly string _valueName;
        private object _currentValue;
        private Type _valueType;
        private readonly IFormStateProvider _formStateProvider;
        private readonly BindingContext _bindingContext;

        public FormParameterSupplier(Type valueType, string valueName, IFormStateProvider formStateProvider, BindingContext _bindingContext)
        {
            _valueType = valueType;
            _valueName = valueName;
            _formStateProvider = formStateProvider;
            this._bindingContext = _bindingContext;
        }

        public object? CurrentValue => _currentValue ??= BindValue();

        [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        [UnconditionalSuppressMessage("Trimming", "IL2077", Justification = "Model types are expected to be defined in assemblies that do not get trimmed.")]
        private object BindValue()
        {
            var props = _valueType.GetProperties();
            var instance = CreateInstance();
            if (instance == null)
            {
                return null;
            }

            foreach (var prop in props)
            {
                try
                {
                    if (_formStateProvider.Fields.TryGetValue(prop.Name, out var value))
                    {
                        prop.SetValue(instance, Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture));
                    }
                }
                catch (Exception ex)
                {
                    _bindingContext.AddBindingError(prop.Name, ex.Message);
                }
            }

            return instance;
        }

        private object CreateInstance()
        {
            try
            {
                return Activator.CreateInstance(_valueType);
            }
            catch (Exception ex)
            {
                _bindingContext.AddBindingError("", ex.Message);
            }

            return null;
        }

        public bool CurrentValueIsFixed => true;

        public bool CanSupplyValue(Type valueType, string? valueName, string? source)
        {
            throw new NotImplementedException();
        }

        public void Subscribe(ComponentState subscriber)
        {
            throw new InvalidOperationException("Form values are always fixed.");
        }

        public void Unsubscribe(ComponentState subscriber)
        {
            throw new InvalidOperationException("Form values are always fixed.");
        }
    }

    private class QueryParameterSupplier : ICascadingValueComponent
    {
        private HashSet<ComponentState>? _subscribers; // Lazily instantiated
        private readonly NavigationManager _navigation;
        private readonly string _valueName;
        private string _currentValue;

        public QueryParameterSupplier(NavigationManager navigation, string valueName)
        {
            _navigation = navigation;
            _valueName = valueName;
            _navigation.LocationChanged += LocationChanged;
            RefreshParameter(_navigation.Uri);
        }

        private void LocationChanged(object? sender, LocationChangedEventArgs e)
        {
            var url = _navigation.Uri;
            RefreshParameter(url);
        }

        private void RefreshParameter(string url)
        {
            ReadOnlyMemory<char> query = default;
            var queryStartPos = url.IndexOf('?');
            if (queryStartPos >= 0)
            {
                var queryEndPos = url.IndexOf('#', queryStartPos);
                query = url.AsMemory(queryStartPos..(queryEndPos < 0 ? url.Length : queryEndPos));
            }
            var enumerable = new QueryStringEnumerable(query);
            var previousValue = _currentValue;
            foreach (var item in enumerable)
            {
                if (item.DecodeName().Span.Equals(_valueName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    _currentValue = item.DecodeValue().ToString();
                }
            }
            if (_subscribers != null && ChangeDetection.MayHaveChanged(previousValue, _currentValue))
            {
                NotifySubscribers(ParameterViewLifetime.Unbound);
            }
        }

        public object? CurrentValue => _currentValue;

        public bool CurrentValueIsFixed => false;

        public bool CanSupplyValue(Type valueType, string? valueName, string? source)
        {
            throw new NotImplementedException();
        }

        private void NotifySubscribers(in ParameterViewLifetime lifetime)
        {
            foreach (var subscriber in _subscribers!)
            {
                subscriber.NotifyCascadingValueChanged(lifetime);
            }
        }

        public void Subscribe(ComponentState subscriber)
        {
            _subscribers ??= new();
            _subscribers.Add(subscriber);
        }

        public void Unsubscribe(ComponentState subscriber)
        {
            _subscribers?.Remove(subscriber);
        }
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        if (!_hasRendered)
        {
            _hasRendered = true;
            _bindingContext = new BindingContext(new Uri(Navigation.Uri).AbsolutePath);
            parameters.SetParameterProperties(this);
            _handler.Render(builder =>
            {
                builder.OpenComponent<CascadingValue<BindingContext>>(0);
                builder.AddComponentParameter(1, "IsFixed", true);
                builder.AddComponentParameter(2, "Value", _bindingContext);
                builder.AddComponentParameter(3, "ChildContent", ChildContent);
                builder.CloseComponent();
            });
        }

        return Task.CompletedTask;
    }

    void ICascadingValueComponent.Subscribe(ComponentState subscriber)
    {
        // throw new NotImplementedException();
    }

    void ICascadingValueComponent.Unsubscribe(ComponentState subscriber)
    {
        // throw new NotImplementedException();
    }
}
