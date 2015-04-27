// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents a <see cref="IValueProvider"/> whose values come from a collection of <see cref="IValueProvider"/>s.
    /// </summary>
    public class CompositeValueProvider :
        Collection<IValueProvider>, 
        IEnumerableValueProvider, 
        IBindingSourceValueProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompositeValueProvider"/>.
        /// </summary>
        public CompositeValueProvider()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompositeValueProvider"/>.
        /// </summary>
        /// <param name="valueProviders">The sequence of <see cref="IValueProvider"/> to add to this instance of
        /// <see cref="CompositeValueProvider"/>.</param>
        public CompositeValueProvider(IEnumerable<IValueProvider> valueProviders)
            : base(valueProviders.ToList())
        {
        }

        /// <summary>
        /// Creates a new <see cref="CompositeValueProvider"/> from the provided <paramref name="context"/>
        /// and <paramref name="factories"/>.
        /// </summary>
        /// <param name="factories">The set of <see cref="IValueProviderFactory"/> instances.</param>
        /// <param name="context">The <see cref="ValueProviderFactoryContext"/>.</param>
        /// <returns>
        /// A <see cref="CompositeValueProvider"/> containing all <see cref="IValueProvider"/> instances
        /// created.
        /// </returns>
        public static CompositeValueProvider Create(
            [NotNull] IEnumerable<IValueProviderFactory> factories, 
            [NotNull] ValueProviderFactoryContext context)
        {
            var composite = new CompositeValueProvider();
            foreach (var valueProvidersFactory in factories)
            {
                var valueProvider = valueProvidersFactory.GetValueProvider(context);
                if (valueProvider != null)
                {
                    composite.Add(valueProvider);
                }
            }

            return composite;
        }

        /// <inheritdoc />
        public virtual async Task<bool> ContainsPrefixAsync(string prefix)
        {
            for (var i = 0; i < Count; i++)
            {
                if (await this[i].ContainsPrefixAsync(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public virtual async Task<ValueProviderResult> GetValueAsync(string key)
        {
            // Performance-sensitive
            // Caching the count is faster for IList<T>
            var itemCount = Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var valueProvider = Items[i];
                var result = await valueProvider.GetValueAsync(key);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <inheritdoc />
        public virtual async Task<IDictionary<string, string>> GetKeysFromPrefixAsync(string prefix)
        {
            foreach (var valueProvider in this)
            {
                var enumeratedProvider = valueProvider as IEnumerableValueProvider;
                if (enumeratedProvider != null)
                {
                    var result = await enumeratedProvider.GetKeysFromPrefixAsync(prefix);
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                }
            }
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, [NotNull] IValueProvider item)
        {
            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, [NotNull] IValueProvider item)
        {
            base.SetItem(index, item);
        }

        /// <inheritdoc />
        public IValueProvider Filter(BindingSource bindingSource)
        {
            var filteredValueProviders = new List<IValueProvider>();
            foreach (var valueProvider in this.OfType<IBindingSourceValueProvider>())
            {
                var result = valueProvider.Filter(bindingSource);
                if (result != null)
                {
                    filteredValueProviders.Add(result);
                }
            }

            return new CompositeValueProvider(filteredValueProviders);
        }
    }
}
