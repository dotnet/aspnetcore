// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Binding info which represents metadata associated to an action parameter.
    /// </summary>
    public class BindingInfo
    {
        /// <summary>
        /// Creates a new <see cref="BindingInfo"/>.
        /// </summary>
        public BindingInfo()
        {
        }

        /// <summary>
        /// Creates a copy of a <see cref="BindingInfo"/>.
        /// </summary>
        /// <param name="other">The <see cref="BindingInfo"/> to copy.</param>
        public BindingInfo(BindingInfo other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            BindingSource = other.BindingSource;
            BinderModelName = other.BinderModelName;
            BinderType = other.BinderType;
            PropertyFilterProvider = other.PropertyFilterProvider;
            RequestPredicate = other.RequestPredicate;
        }

        /// <summary>
        /// Gets or sets the <see cref="ModelBinding.BindingSource"/>.
        /// </summary>
        public BindingSource BindingSource { get; set; }

        /// <summary>
        /// Gets or sets the binder model name.
        /// </summary>
        public string BinderModelName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> of the model binder used to bind the model.
        /// </summary>
        public Type BinderType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelBinding.IPropertyFilterProvider"/>.
        /// </summary>
        public IPropertyFilterProvider PropertyFilterProvider { get; set; }

        /// <summary>
        /// Gets or sets a predicate which determines whether or not the model should be bound based on state
        /// from the current request.
        /// </summary>
        public Func<ActionContext, bool> RequestPredicate { get; set; }

        /// <summary>
        /// Constructs a new instance of <see cref="BindingInfo"/> from the given <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">A collection of attributes which are used to construct <see cref="BindingInfo"/>
        /// </param>
        /// <returns>A new instance of <see cref="BindingInfo"/>.</returns>
        public static BindingInfo GetBindingInfo(IEnumerable<object> attributes)
        {
            var bindingInfo = new BindingInfo();
            var isBindingInfoPresent = false;

            // BinderModelName
            foreach (var binderModelNameAttribute in attributes.OfType<IModelNameProvider>())
            {
                isBindingInfoPresent = true;
                if (binderModelNameAttribute?.Name != null)
                {
                    bindingInfo.BinderModelName = binderModelNameAttribute.Name;
                    break;
                }
            }

            // BinderType
            foreach (var binderTypeAttribute in attributes.OfType<IBinderTypeProviderMetadata>())
            {
                isBindingInfoPresent = true;
                if (binderTypeAttribute.BinderType != null)
                {
                    bindingInfo.BinderType = binderTypeAttribute.BinderType;
                    break;
                }
            }

            // BindingSource
            foreach (var bindingSourceAttribute in attributes.OfType<IBindingSourceMetadata>())
            {
                isBindingInfoPresent = true;
                if (bindingSourceAttribute.BindingSource != null)
                {
                    bindingInfo.BindingSource = bindingSourceAttribute.BindingSource;
                    break;
                }
            }

            // PropertyFilterProvider
            var propertyFilterProviders = attributes.OfType<IPropertyFilterProvider>().ToArray();
            if (propertyFilterProviders.Length == 1)
            {
                isBindingInfoPresent = true;
                bindingInfo.PropertyFilterProvider = propertyFilterProviders[0];
            }
            else if (propertyFilterProviders.Length > 1)
            {
                isBindingInfoPresent = true;
                bindingInfo.PropertyFilterProvider = new CompositePropertyFilterProvider(propertyFilterProviders);
            }

            // RequestPredicate
            foreach (var requestPredicateProvider in attributes.OfType<IRequestPredicateProvider>())
            {
                isBindingInfoPresent = true;
                if (requestPredicateProvider.RequestPredicate != null)
                {
                    bindingInfo.RequestPredicate = requestPredicateProvider.RequestPredicate;
                    break;
                }
            }

            return isBindingInfoPresent ? bindingInfo : null;
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