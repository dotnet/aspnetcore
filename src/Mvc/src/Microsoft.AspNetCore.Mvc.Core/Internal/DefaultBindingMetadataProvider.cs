// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A default implementation of <see cref="IBindingMetadataProvider"/>.
    /// </summary>
    public class DefaultBindingMetadataProvider : IBindingMetadataProvider
    {
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // BinderModelName
            foreach (var binderModelNameAttribute in context.Attributes.OfType<IModelNameProvider>())
            {
                if (binderModelNameAttribute?.Name != null)
                {
                    context.BindingMetadata.BinderModelName = binderModelNameAttribute.Name;
                    break;
                }
            }

            // BinderType
            foreach (var binderTypeAttribute in context.Attributes.OfType<IBinderTypeProviderMetadata>())
            {
                if (binderTypeAttribute.BinderType != null)
                {
                    context.BindingMetadata.BinderType = binderTypeAttribute.BinderType;
                    break;
                }
            }

            // BindingSource
            foreach (var bindingSourceAttribute in context.Attributes.OfType<IBindingSourceMetadata>())
            {
                if (bindingSourceAttribute.BindingSource != null)
                {
                    context.BindingMetadata.BindingSource = bindingSourceAttribute.BindingSource;
                    break;
                }
            }

            // PropertyFilterProvider
            var propertyFilterProviders = context.Attributes.OfType<IPropertyFilterProvider>().ToArray();
            if (propertyFilterProviders.Length == 0)
            {
                context.BindingMetadata.PropertyFilterProvider = null;
            }
            else if (propertyFilterProviders.Length == 1)
            {
                context.BindingMetadata.PropertyFilterProvider = propertyFilterProviders[0];
            }
            else
            {
                var composite = new CompositePropertyFilterProvider(propertyFilterProviders);
                context.BindingMetadata.PropertyFilterProvider = composite;
            }

            var bindingBehavior = FindBindingBehavior(context);
            if (bindingBehavior != null)
            {
                context.BindingMetadata.IsBindingAllowed = bindingBehavior.Behavior != BindingBehavior.Never;
                context.BindingMetadata.IsBindingRequired = bindingBehavior.Behavior == BindingBehavior.Required;
            }
        }

        private static BindingBehaviorAttribute FindBindingBehavior(BindingMetadataProviderContext context)
        {
            switch (context.Key.MetadataKind)
            {
                case ModelMetadataKind.Property:
                    // BindingBehavior can fall back to attributes on the Container Type, but we should ignore
                    // attributes on the Property Type.
                    var matchingAttributes = context.PropertyAttributes.OfType<BindingBehaviorAttribute>();
                    return matchingAttributes.FirstOrDefault()
                        ?? context.Key.ContainerType.GetTypeInfo()
                            .GetCustomAttributes(typeof(BindingBehaviorAttribute), inherit: true)
                            .OfType<BindingBehaviorAttribute>()
                            .FirstOrDefault();
                case ModelMetadataKind.Parameter:
                    return context.ParameterAttributes.OfType<BindingBehaviorAttribute>().FirstOrDefault();
                default:
                    return null;
            }
        }

        private class CompositePropertyFilterProvider : IPropertyFilterProvider
        {
            private readonly IEnumerable<IPropertyFilterProvider> _providers;

            public CompositePropertyFilterProvider(IEnumerable<IPropertyFilterProvider> providers)
            {
                _providers = providers;
            }

            public Func<ModelMetadata, bool> PropertyFilter => CreatePropertyFilter();

            private Func<ModelMetadata, bool> CreatePropertyFilter()
            {
                var propertyFilters = _providers
                    .Select(p => p.PropertyFilter)
                    .Where(p => p != null);

                return (m) =>
                {
                    foreach (var propertyFilter in propertyFilters)
                    {
                        if (!propertyFilter(m))
                        {
                            return false;
                        }
                    }

                    return true;
                };
            }
        }
    }
}