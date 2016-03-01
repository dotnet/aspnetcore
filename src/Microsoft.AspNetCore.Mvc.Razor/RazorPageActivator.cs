// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <inheritdoc />
    public class RazorPageActivator : IRazorPageActivator
    {
        // Name of the "public TModel Model" property on RazorPage<TModel>
        private const string ModelPropertyName = "Model";
        private readonly ConcurrentDictionary<Type, PageActivationInfo> _activationInfo;
        private readonly IModelMetadataProvider _metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorPageActivator"/> class.
        /// </summary>
        public RazorPageActivator(IModelMetadataProvider metadataProvider)
        {
            _activationInfo = new ConcurrentDictionary<Type, PageActivationInfo>();
            _metadataProvider = metadataProvider;
        }

        /// <inheritdoc />
        public void Activate(IRazorPage page, ViewContext context)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var activationInfo = _activationInfo.GetOrAdd(page.GetType(),
                                                          CreateViewActivationInfo);

            context.ViewData = CreateViewDataDictionary(context, activationInfo);

            for (var i = 0; i < activationInfo.PropertyActivators.Length; i++)
            {
                var activateInfo = activationInfo.PropertyActivators[i];
                activateInfo.Activate(page, context);
            }
        }

        private ViewDataDictionary CreateViewDataDictionary(ViewContext context, PageActivationInfo activationInfo)
        {
            // Create a ViewDataDictionary<TModel> if the ViewContext.ViewData is not set or the type of
            // ViewContext.ViewData is an incompatibile type.
            if (context.ViewData == null)
            {
                // Create ViewDataDictionary<TModel>(IModelMetadataProvider, ModelStateDictionary).
                return (ViewDataDictionary)Activator.CreateInstance(
                    activationInfo.ViewDataDictionaryType,
                    _metadataProvider,
                    context.ModelState);
            }
            else if (context.ViewData.GetType() != activationInfo.ViewDataDictionaryType)
            {
                // Create ViewDataDictionary<TModel>(ViewDataDictionary).
                return (ViewDataDictionary)Activator.CreateInstance(
                    activationInfo.ViewDataDictionaryType,
                    context.ViewData);
            }

            return context.ViewData;
        }

        private PageActivationInfo CreateViewActivationInfo(Type type)
        {
            // Look for a property named "Model". If it is non-null, we'll assume this is
            // the equivalent of TModel Model property on RazorPage<TModel>
            var modelProperty = type.GetRuntimeProperty(ModelPropertyName);
            if (modelProperty == null)
            {
                var message = Resources.FormatViewCannotBeActivated(type.FullName, GetType().FullName);
                throw new InvalidOperationException(message);
            }

            var modelType = modelProperty.PropertyType;
            var viewDataType = typeof(ViewDataDictionary<>).MakeGenericType(modelType);

            return new PageActivationInfo
            {
                ViewDataDictionaryType = viewDataType,
                PropertyActivators = PropertyActivator<ViewContext>.GetPropertiesToActivate(
                    type,
                    typeof(RazorInjectAttribute),
                    CreateActivateInfo,
                    includeNonPublic: true)
            };
        }

        private PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
        {
            Func<ViewContext, object> valueAccessor;
            if (typeof(ViewDataDictionary).IsAssignableFrom(property.PropertyType))
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
                valueAccessor = context =>
                {
                    var serviceProvider = context.HttpContext.RequestServices;
                    var factory = serviceProvider.GetRequiredService<IUrlHelperFactory>();
                    return factory.GetUrlHelper(context);
                };
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

        private class PageActivationInfo
        {
            public PropertyActivator<ViewContext>[] PropertyActivators { get; set; }

            public Type ViewDataDictionaryType { get; set; }

            public Action<object, object> ViewDataDictionarySetter { get; set; }
        }
    }
}