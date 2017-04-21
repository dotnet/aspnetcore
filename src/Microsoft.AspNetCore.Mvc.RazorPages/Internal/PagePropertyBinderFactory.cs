// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PagePropertyBinderFactory
    {
        public static Func<Page, object, Task> CreateBinder(
            ParameterBinder parameterBinder,
            IModelMetadataProvider modelMetadataProvider,
            CompiledPageActionDescriptor actionDescriptor)
        {
            if (parameterBinder == null)
            {
                throw new ArgumentNullException(nameof(parameterBinder));
            }

            if (actionDescriptor == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptor));
            }

            var properties = actionDescriptor.BoundProperties;
            if (properties == null || properties.Count == 0)
            {
                return null;
            }

            var isHandlerThePage = actionDescriptor.HandlerTypeInfo == actionDescriptor.PageTypeInfo;
            
            var type = actionDescriptor.HandlerTypeInfo.AsType();
            var metadata = new ModelMetadata[properties.Count];
            for (var i = 0; i < properties.Count; i++)
            {
                metadata[i] = modelMetadataProvider.GetMetadataForProperty(type, properties[i].Name);
            }

            return Bind;

            Task Bind(Page page, object model)
            {
                if (page == null)
                {
                    throw new ArgumentNullException(nameof(page));
                }

                if (!isHandlerThePage && model == null)
                {
                    throw new ArgumentNullException(nameof(model));
                }

                var pageContext = page.PageContext;
                var instance = isHandlerThePage ? page : model;
                return BindPropertiesAsync(parameterBinder, pageContext, instance, properties, metadata);
            }
        }

        private static async Task BindPropertiesAsync(
            ParameterBinder parameterBinder,
            PageContext pageContext,
            object instance,
            IList<ParameterDescriptor> properties,
            IList<ModelMetadata> metadata)
        {
            var isGet = string.Equals("GET", pageContext.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase);

            var valueProvider = await CompositeValueProvider.CreateAsync(pageContext, pageContext.ValueProviderFactories);
            for (var i = 0; i < properties.Count; i++)
            {
                if (isGet && !((PageBoundPropertyDescriptor)properties[i]).SupportsGet)
                {
                    continue;
                }

                var result = await parameterBinder.BindModelAsync(pageContext, valueProvider, properties[i]);
                if (result.IsModelSet)
                {
                    PropertyValueSetter.SetValue(metadata[i], instance, result.Model);
                }
            }
        }
    }
}
