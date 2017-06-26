// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public static class PagePropertyBinderFactory
    {
        public static Func<PageContext, object, Task> CreateBinder(
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

            var type = actionDescriptor.HandlerTypeInfo.AsType();
            var metadata = new ModelMetadata[properties.Count];
            for (var i = 0; i < properties.Count; i++)
            {
                metadata[i] = modelMetadataProvider.GetMetadataForProperty(type, properties[i].Name);
            }

            return Bind;

            Task Bind(PageContext pageContext, object instance)
            {
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
            var valueProvider = await CompositeValueProvider.CreateAsync(pageContext, pageContext.ValueProviderFactories);
            for (var i = 0; i < properties.Count; i++)
            {
                var result = await parameterBinder.BindModelAsync(pageContext, valueProvider, properties[i]);
                if (result.IsModelSet)
                {
                    PropertyValueSetter.SetValue(metadata[i], instance, result.Model);
                }
            }
        }
    }
}
