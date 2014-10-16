// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <inheritdoc />
    public class DefaultTagHelperActivator : ITagHelperActivator
    {
        private readonly ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]> _injectActions;
        private readonly Func<Type, PropertyActivator<ViewContext>[]> _getPropertiesToActivate;

        /// <summary>
        /// Instantiates a new <see cref="DefaultTagHelperActivator"/> instance.
        /// </summary>
        public DefaultTagHelperActivator()
        {
            _injectActions = new ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]>();
            _getPropertiesToActivate = type =>
                PropertyActivator<ViewContext>.GetPropertiesToActivate(
                    type, typeof(ActivateAttribute), CreateActivateInfo);
        }

        /// <inheritdoc />
        public void Activate([NotNull] ITagHelper tagHelper, [NotNull] ViewContext context)
        {
            var propertiesToActivate = _injectActions.GetOrAdd(tagHelper.GetType(),
                                                               _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(tagHelper, context);
            }
        }

        private static PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
        {
            Func<ViewContext, object> valueAccessor;

            if (property.PropertyType == typeof(ViewContext))
            {
                valueAccessor = viewContext => viewContext;
            }
            else
            {
                valueAccessor = (viewContext) =>
                {
                    var serviceProvider = viewContext.HttpContext.RequestServices;
                    var service = serviceProvider.GetRequiredService(property.PropertyType);

                    var contextable = service as ICanHasViewContext;
                    contextable?.Contextualize(viewContext);

                    return service;
                };
            }

            return new PropertyActivator<ViewContext>(property, valueAccessor);
        }
    }
}