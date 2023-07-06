// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Defines the binding context for data bound from external sources.
/// </summary>
public sealed class CascadingModelBinder : SupplyParameterFromFormValueProvider, IComponent, IDisposable
{
    private RenderHandle _handle;
    private bool _hasPendingQueuedRender;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="CascadingModelBinder"/>.
    /// </summary>
    [Parameter] public RenderFragment<ModelBindingContext> ChildContent { get; set; } = default!;

    [CascadingParameter] ModelBindingContext? ParentContext { get; set; }

    [Inject] internal NavigationManager Navigation { get; set; } = null!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (BindingContext == null)
        {
            // First render
            Navigation.LocationChanged += HandleLocationChanged;
        }

        parameters.SetParameterProperties(this);
        if (ParentContext != null && string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"Nested binding contexts must define a Name. (Parent context) = '{ParentContext.Name}'.");
        }

        UpdateBindingInformation();
        Render();

        return Task.CompletedTask;
    }

    private void Render()
    {
        if (_hasPendingQueuedRender)
        {
            return;
        }
        _hasPendingQueuedRender = true;
        _handle.Render(builder =>
        {
            _hasPendingQueuedRender = false;
            builder.AddContent(0, ChildContent, BindingContext!);
        });
    }

    public void UpdateBindingInformation()
    {
        UpdateBindingInformation(Navigation, ParentContext, Name);
    }

    // TODO: Don't think we have to do this really
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var url = e.Location;
        UpdateBindingInformation();
        Render();
    }

    void IDisposable.Dispose()
    {
        Navigation.LocationChanged -= HandleLocationChanged;
    }
}

public static class SupplyParameterFromFormServiceCollectionExtensions
{
    public static IServiceCollection AddSupplyValueFromFormProvider(this IServiceCollection serviceCollection)
    {
        return serviceCollection.AddScoped<ICascadingValueSupplier, SupplyParameterFromFormValueProvider>(services =>
        {
            var result = new SupplyParameterFromFormValueProvider();

            if (services.GetService<CascadingModelBindingProvider>() is not null)
            {
                result.ModelBindingProviders = services.GetServices<CascadingModelBindingProvider>();
            }

            result.UpdateBindingInformation(
                services.GetRequiredService<NavigationManager>(), null, "");

            return result;
        });
    }
}

// TODO: Make this internal by changing CascadingModelBinder so it doesn't inherit from it, but instead
// implements ICascadingValueSupplier by forwarding all calls to an instance of this
public class SupplyParameterFromFormValueProvider : ICascadingValueSupplier
{
    private readonly Dictionary<Type, CascadingModelBindingProvider?> _providersByCascadingParameterAttributeType = new();

    [Inject] internal IEnumerable<CascadingModelBindingProvider> ModelBindingProviders { get; set; } = Enumerable.Empty<CascadingModelBindingProvider>();

    public bool IsFixed => true;

    private ModelBindingContext? _bindingContext;

    protected internal ModelBindingContext? BindingContext => _bindingContext;

    public void UpdateBindingInformation(NavigationManager navigation, ModelBindingContext? parentContext, string thisName)
    {
        // BindingContextId: action parameter used to define the handler
        // Name: form name and context used to bind
        // Cases:
        // 1) No name ("")
        // Name = "";
        // BindingContextId = "";
        // <form name="" action="" />
        // 2) Name provided
        // Name = "my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        // <form name="my-handler" action="relative/path?existing=value&handler=my-handler
        // 3) Parent has a name "parent-name"
        // Name = "parent-name.my-handler";
        // BindingContextId = <<base-relative-uri>>((<<existing-query>>&)|?)handler=my-handler
        var name = ModelBindingContext.Combine(parentContext, thisName);
        var bindingId = string.IsNullOrEmpty(name) ? "" : GenerateBindingContextId(name);
        var bindingContextDidChange =
            _bindingContext is null ||
            !string.Equals(_bindingContext.Name, name, StringComparison.Ordinal) ||
            !string.Equals(_bindingContext.BindingContextId, bindingId, StringComparison.Ordinal);

        if (bindingContextDidChange)
        {
            if (IsFixed && _bindingContext is not null)
            {
                // Throw an exception if either the Name or the BindingContextId changed. Once a CascadingModelBinder has been initialized
                // as fixed, it can't change it's name nor its BindingContextId. This can happen in several situations:
                // * Component ParentContext hierarchy changes.
                //   * Technically, the component won't be retained in this case and will be destroyed instead.
                // * A parent changes Name.
                throw new InvalidOperationException($"'{nameof(CascadingModelBinder)}' 'Name' can't change after initialized.");
            }

            _bindingContext = new ModelBindingContext(name, bindingId);
            parentContext?.SetErrors(name, _bindingContext);
        }

        string GenerateBindingContextId(string name)
        {
            var bindingId = navigation.ToBaseRelativePath(navigation.GetUriWithQueryParameter("handler", name));
            var hashIndex = bindingId.IndexOf('#');
            return hashIndex == -1 ? bindingId : new string(bindingId.AsSpan(0, hashIndex));
        }
    }

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return true;
        }

        // We also supply values for [SupplyValueFromForm]
        return TryGetProvider(in parameterInfo, out var provider) && provider.CanSupplyValue(_bindingContext, parameterInfo);
    }

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
    {
        // We supply a ModelBindingContext
        if (parameterInfo.Attribute is CascadingParameterAttribute && parameterInfo.PropertyType == typeof(ModelBindingContext))
        {
            return _bindingContext;
        }

        // We also supply values for [SupplyValueFromForm]
        return TryGetProvider(in parameterInfo, out var provider)
            ? provider.GetCurrentValue(_bindingContext, parameterInfo)
            : null;
    }

    private CascadingModelBindingProvider GetProviderOrThrow(in CascadingParameterInfo parameterInfo)
    {
        if (!TryGetProvider(parameterInfo, out var provider))
        {
            throw new InvalidOperationException($"No model binding provider could be found for parameter '{parameterInfo.PropertyName}'.");
        }

        return provider;
    }

    private bool TryGetProvider(in CascadingParameterInfo parameterInfo, [NotNullWhen(true)] out CascadingModelBindingProvider? result)
    {
        var attributeType = parameterInfo.Attribute.GetType();

        if (_providersByCascadingParameterAttributeType.TryGetValue(attributeType, out result))
        {
            return result is not null;
        }

        // We deliberately cache 'null' results to avoid searching for the same attribute type multiple times.
        result = FindProviderForAttributeType(attributeType);
        _providersByCascadingParameterAttributeType[attributeType] = result;
        return result is not null;

        CascadingModelBindingProvider? FindProviderForAttributeType(Type attributeType)
        {
            foreach (var provider in ModelBindingProviders)
            {
                if (provider.SupportsCascadingParameterAttributeType(attributeType))
                {
                    return provider;
                }
            }

            return null;
        }
    }
}
