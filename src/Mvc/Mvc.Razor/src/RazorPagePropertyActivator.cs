// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor;

internal sealed class RazorPagePropertyActivator
{
    private readonly IModelMetadataProvider _metadataProvider;
    private readonly Func<IModelMetadataProvider, ModelStateDictionary, ViewDataDictionary> _rootFactory;
    private readonly Func<ViewDataDictionary, ViewDataDictionary> _nestedFactory;
    private readonly Type _viewDataDictionaryType;
    private readonly PropertyActivator<ViewContext>[] _propertyActivators;

    public RazorPagePropertyActivator(
        Type pageType,
        Type? declaredModelType,
        IModelMetadataProvider metadataProvider,
        PropertyValueAccessors propertyValueAccessors)
    {
        _metadataProvider = metadataProvider;

        // In the absence of a model on the current type, we'll attempt to use ViewDataDictionary<object> on the current type.
        var viewDataDictionaryModelType = declaredModelType ?? typeof(object);

        _viewDataDictionaryType = typeof(ViewDataDictionary<>).MakeGenericType(viewDataDictionaryModelType);
        _rootFactory = ViewDataDictionaryFactory.CreateFactory(viewDataDictionaryModelType);
        _nestedFactory = ViewDataDictionaryFactory.CreateNestedFactory(viewDataDictionaryModelType);
        _propertyActivators = PropertyActivator<ViewContext>.GetPropertiesToActivate<RazorInjectAttribute>(
            pageType,
            (propertyInfo, attribute) => CreateActivateInfo(propertyInfo, attribute, propertyValueAccessors),
            includeNonPublic: true);
    }

    public void Activate(object page, ViewContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ViewData = CreateViewDataDictionary(context);
        for (var i = 0; i < _propertyActivators.Length; i++)
        {
            var activateInfo = _propertyActivators[i];
            activateInfo.Activate(page, context);
        }
    }

    // Internal for unit testing.
    internal ViewDataDictionary CreateViewDataDictionary(ViewContext context)
    {
        // Create a ViewDataDictionary<TModel> if the ViewContext.ViewData is not set or the type of
        // ViewContext.ViewData is an incompatible type.
        if (context.ViewData == null)
        {
            // Create ViewDataDictionary<TModel>(IModelMetadataProvider, ModelStateDictionary).
            return _rootFactory(_metadataProvider, context.ModelState);
        }
        else if (context.ViewData.GetType() != _viewDataDictionaryType)
        {
            // Create ViewDataDictionary<TModel>(ViewDataDictionary).
            return _nestedFactory(context.ViewData);
        }

        return context.ViewData;
    }

    private static PropertyActivator<ViewContext> CreateActivateInfo(
        PropertyInfo property,
        RazorInjectAttribute attribute,
        PropertyValueAccessors valueAccessors)
    {
        Func<ViewContext, object> valueAccessor;
        if (attribute.Key is { } key)
        {
            valueAccessor = context =>
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var value = serviceProvider.GetRequiredKeyedService(property.PropertyType, key);
                (value as IViewContextAware)?.Contextualize(context);

                return value;
            };
        }
        else if (typeof(ViewDataDictionary).IsAssignableFrom(property.PropertyType))
        {
            // Logic looks reversed in condition above but is OK. Support only properties of base
            // ViewDataDictionary type and activationInfo.ViewDataDictionaryType. VDD<AnotherType> will fail when
            // assigning to the property (InvalidCastException) and that's fine.
            valueAccessor = context => context.ViewData;
        }
        else if (property.PropertyType == typeof(IUrlHelper))
        {
            // W.r.t. specificity of above condition: Users are much more likely to inject their own
            // IUrlHelperFactory than to create a class implementing IUrlHelper (or a sub-interface) and inject
            // that. But the second scenario is supported. (Note the class must implement ICanHasViewContext.)
            valueAccessor = valueAccessors.UrlHelperAccessor;
        }
        else if (property.PropertyType == typeof(IJsonHelper))
        {
            valueAccessor = valueAccessors.JsonHelperAccessor;
        }
        else if (property.PropertyType == typeof(DiagnosticSource))
        {
            valueAccessor = valueAccessors.DiagnosticSourceAccessor;
        }
        else if (property.PropertyType == typeof(HtmlEncoder))
        {
            valueAccessor = valueAccessors.HtmlEncoderAccessor;
        }
        else if (property.PropertyType == typeof(IModelExpressionProvider))
        {
            valueAccessor = valueAccessors.ModelExpressionProviderAccessor;
        }
        else
        {
            valueAccessor = context =>
            {
                var serviceProvider = context.HttpContext.RequestServices;
                var value = serviceProvider.GetRequiredService(property.PropertyType);
                (value as IViewContextAware)?.Contextualize(context);

                return value;
            };
        }

        return new PropertyActivator<ViewContext>(property, valueAccessor);
    }

    public sealed class PropertyValueAccessors
    {
        public Func<ViewContext, object> UrlHelperAccessor { get; init; } = default!;

        public Func<ViewContext, object> JsonHelperAccessor { get; init; } = default!;

        public Func<ViewContext, object> DiagnosticSourceAccessor { get; init; } = default!;

        public Func<ViewContext, object> HtmlEncoderAccessor { get; init; } = default!;

        public Func<ViewContext, object> ModelExpressionProviderAccessor { get; init; } = default!;
    }
}
