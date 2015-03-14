// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    /// <summary>
    /// Represents the <see cref="IViewComponentActivator"/> that is registered by default.
    /// </summary>
    public class DefaultViewComponentActivator : IViewComponentActivator
    {
        private readonly Func<Type, PropertyActivator<ViewContext>[]> _getPropertiesToActivate;
        private readonly IDictionary<Type, Func<ViewContext, object>> _valueAccessorLookup;
        private readonly ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]> _injectActions;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultViewComponentActivator"/> class.
        /// </summary>
        public DefaultViewComponentActivator()
        {
            _valueAccessorLookup = CreateValueAccessorLookup();
            _injectActions = new ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]>();
            _getPropertiesToActivate = type =>
                PropertyActivator<ViewContext>.GetPropertiesToActivate(
                    type, typeof(ActivateAttribute), CreateActivateInfo);
        }

        /// <inheritdoc />
        public virtual void Activate([NotNull] object viewComponent, [NotNull] ViewContext context)
        {
            var propertiesToActivate = _injectActions.GetOrAdd(viewComponent.GetType(),
                                                               _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(viewComponent, context);
            }
        }

        /// <summary>
        /// Creates a lookup dictionary for the values to be activated.
        /// </summary>
        /// <returns>Returns a readonly dictionary of the values corresponding to the types.</returns>
        protected virtual IDictionary<Type, Func<ViewContext, object>> CreateValueAccessorLookup()
        {
            return new Dictionary<Type, Func<ViewContext, object>>
            {
                { typeof(ViewContext), (context) => context },
                {
                    typeof(ViewDataDictionary),
                    (context) =>
                    {
                        return new ViewDataDictionary(context.ViewData);
                    }
                }
            };
        }

        private PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
        {
            Func<ViewContext, object> valueAccessor;
            if (!_valueAccessorLookup.TryGetValue(property.PropertyType, out valueAccessor))
            {
                valueAccessor = (viewContext) =>
                {
                    var serviceProvider = viewContext.HttpContext.RequestServices;
                    var service = serviceProvider.GetRequiredService(property.PropertyType);
                    if (typeof(ICanHasViewContext).IsAssignableFrom(property.PropertyType))
                    {
                        ((ICanHasViewContext)service).Contextualize(viewContext);
                    }

                    return service;
                };
            }

            return new PropertyActivator<ViewContext>(property, valueAccessor);
        }
    }
}