// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class RazorPageActivator : IRazorPageActivator
    {
        // Name of the "public TModel Model" property on RazorPage<TModel>
        private const string ModelPropertyName = "Model";
        private readonly ITypeActivator _typeActivator;
        private readonly ConcurrentDictionary<Type, PageActivationInfo> _activationInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorPageActivator"/> class.
        /// </summary>
        public RazorPageActivator(ITypeActivator typeActivator)
        {
            _typeActivator = typeActivator;
            _activationInfo = new ConcurrentDictionary<Type, PageActivationInfo>();
        }

        /// <inheritdoc />
        public void Activate([NotNull] IRazorPage page, [NotNull] ViewContext context)
        {
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
                // Create ViewDataDictionary<TModel>(metadataProvider);
                return (ViewDataDictionary)_typeActivator.CreateInstance(context.HttpContext.RequestServices,
                                                                         activationInfo.ViewDataDictionaryType);
            }
            else if (context.ViewData.GetType() != activationInfo.ViewDataDictionaryType)
            {
                // Create ViewDataDictionary<TModel>(ViewDataDictionary);
                return (ViewDataDictionary)_typeActivator.CreateInstance(context.HttpContext.RequestServices,
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
                PropertyActivators = PropertyActivator<ViewContext>.GetPropertiesToActivate(type,
                                                                                            typeof(ActivateAttribute),
                                                                                            CreateActivateInfo)
            };
        }

        private PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
        {
            Func<ViewContext, object> valueAccessor;
            if (typeof(ViewDataDictionary).IsAssignableFrom(property.PropertyType))
            {
                valueAccessor = context => context.ViewData;
            }
            else
            {
                valueAccessor = context =>
               {
                   var serviceProvider = context.HttpContext.RequestServices;
                   var value = serviceProvider.GetRequiredService(property.PropertyType);
                   var canHasViewContext = value as ICanHasViewContext;
                   if (canHasViewContext != null)
                   {
                       canHasViewContext.Contextualize(context);
                   }

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