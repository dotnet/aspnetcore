// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="IBindingMetadataProvider"/>.
    /// </summary>
    public class DefaultBindingMetadataProvider : IBindingMetadataProvider
    {
        /// <inheritdoc />
        public void GetBindingMetadata([NotNull] BindingMetadataProviderContext context)
        {
            // For Model Name  - we only use the first attribute we find. An attribute on the parameter
            // is considered an override of an attribute on the type. This is for compatibility with [Bind]
            // from MVC 5.
            //
            // BinderType and BindingSource fall back to the first attribute to provide a value.

            // BinderModelName
            var binderModelNameAttribute = context.Attributes.OfType<IModelNameProvider>().FirstOrDefault();
            if (binderModelNameAttribute?.Name != null)
            {
                context.BindingMetadata.BinderModelName = binderModelNameAttribute.Name;
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

            // PropertyBindingPredicateProvider
            var predicateProviders = context.Attributes.OfType<IPropertyBindingPredicateProvider>().ToArray();
            if (predicateProviders.Length > 0)
            {
                context.BindingMetadata.PropertyBindingPredicateProvider = new CompositePredicateProvider(
                    predicateProviders);
            }
        }

        private class CompositePredicateProvider : IPropertyBindingPredicateProvider
        {
            private readonly IEnumerable<IPropertyBindingPredicateProvider> _providers;

            public CompositePredicateProvider(IEnumerable<IPropertyBindingPredicateProvider> providers)
            {
                _providers = providers;
            }

            public Func<ModelBindingContext, string, bool> PropertyFilter
            {
                get
                {
                    return CreatePredicate();
                }
            }

            private Func<ModelBindingContext, string, bool> CreatePredicate()
            {
                var predicates = _providers
                    .Select(p => p.PropertyFilter)
                    .Where(p => p != null);

                return (context, propertyName) =>
                {
                    foreach (var predicate in predicates)
                    {
                        if (!predicate(context, propertyName))
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