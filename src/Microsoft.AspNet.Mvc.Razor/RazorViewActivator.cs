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
    public class RazorViewActivator : IRazorViewActivator
    {
        // Name of the "public TModel Model" property on RazorView<TModel>
        private const string ModelPropertyName = "Model";
        private readonly ITypeActivator _typeActivator;
        private readonly ConcurrentDictionary<Type, ViewActivationInfo> _activationInfo;

        /// <summary>
        /// Initializes a new instance of the RazorViewActivator class.
        /// </summary>
        public RazorViewActivator(ITypeActivator typeActivator)
        {
            _typeActivator = typeActivator;
            _activationInfo = new ConcurrentDictionary<Type, ViewActivationInfo>();
        }

        /// <summary>
        /// Activates the specified view by using the specified ViewContext.
        /// </summary>
        /// <param name="view">The view to activate.</param>
        /// <param name="context">The ViewContext for the executing view.</param>
        public void Activate([NotNull] RazorView view, [NotNull] ViewContext context)
        {
            var activationInfo = _activationInfo.GetOrAdd(view.GetType(),
                                                          CreateViewActivationInfo);

            context.ViewData = CreateViewDataDictionary(context, activationInfo);

            for (var i = 0; i < activationInfo.PropertyActivators.Length; i++)
            {
                var activateInfo = activationInfo.PropertyActivators[i];
                activateInfo.Activate(view, context);
            }
        }

        private ViewDataDictionary CreateViewDataDictionary(ViewContext context, ViewActivationInfo activationInfo)
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

        private ViewActivationInfo CreateViewActivationInfo(Type type)
        {
            // Look for a property named "Model". If it is non-null, we'll assume this is 
            // the equivalent of TModel Model property on RazorView<TModel>
            var modelProperty = type.GetRuntimeProperty(ModelPropertyName);
            if (modelProperty == null)
            {
                var message = Resources.FormatViewCannotBeActivated(type.FullName, GetType().FullName);
                throw new InvalidOperationException(message);
            }

            var modelType = modelProperty.PropertyType;
            var viewDataType = typeof(ViewDataDictionary<>).MakeGenericType(modelType);

            return new ViewActivationInfo
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
                   var value = serviceProvider.GetService(property.PropertyType);
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

        private class ViewActivationInfo
        {
            public PropertyActivator<ViewContext>[] PropertyActivators { get; set; }

            public Type ViewDataDictionaryType { get; set; }

            public Action<object, object> ViewDataDictionarySetter { get; set; }
        }
    }
}