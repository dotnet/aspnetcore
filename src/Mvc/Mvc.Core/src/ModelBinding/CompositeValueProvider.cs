// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Represents a <see cref="IValueProvider"/> whose values come from a collection of <see cref="IValueProvider"/>s.
    /// </summary>
    public class CompositeValueProvider :
        Collection<IValueProvider>,
        IEnumerableValueProvider,
        IBindingSourceValueProvider,
        IKeyRewriterValueProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompositeValueProvider"/>.
        /// </summary>
        public CompositeValueProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompositeValueProvider"/>.
        /// </summary>
        /// <param name="valueProviders">The sequence of <see cref="IValueProvider"/> to add to this instance of
        /// <see cref="CompositeValueProvider"/>.</param>
        public CompositeValueProvider(IList<IValueProvider> valueProviders)
            : base(valueProviders)
        {
        }

        /// <summary>
        /// Asynchronously creates a <see cref="CompositeValueProvider"/> using the provided
        /// <paramref name="controllerContext"/>.
        /// </summary>
        /// <param name="controllerContext">The <see cref="ControllerContext"/> associated with the current request.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> which, when completed, asynchronously returns a
        /// <see cref="CompositeValueProvider"/>.
        /// </returns>
        public static async Task<CompositeValueProvider> CreateAsync(ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            var factories = controllerContext.ValueProviderFactories;

            return await CreateAsync(controllerContext, factories);
        }

        /// <summary>
        /// Asynchronously creates a <see cref="CompositeValueProvider"/> using the provided
        /// <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <param name="factories">The <see cref="IValueProviderFactory"/> to be applied to the context.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> which, when completed, asynchronously returns a
        /// <see cref="CompositeValueProvider"/>.
        /// </returns>
        public static async Task<CompositeValueProvider> CreateAsync(
            ActionContext actionContext,
            IList<IValueProviderFactory> factories)
        {
            var valueProviderFactoryContext = new ValueProviderFactoryContext(actionContext);

            for (var i = 0; i < factories.Count; i++)
            {
                var factory = factories[i];
                await factory.CreateValueProviderAsync(valueProviderFactoryContext);
            }

            return new CompositeValueProvider(valueProviderFactoryContext.ValueProviders);
        }

        internal static async ValueTask<(bool success, CompositeValueProvider valueProvider)> TryCreateAsync(
            ActionContext actionContext,
            IList<IValueProviderFactory> factories)
        {
            try
            {
                var valueProvider = await CreateAsync(actionContext, factories);
                return (true, valueProvider);
            }
            catch (ValueProviderException exception)
            {
                actionContext.ModelState.TryAddModelException(key: string.Empty, exception);
                return (false, null);
            }
        }

        /// <inheritdoc />
        public virtual bool ContainsPrefix(string prefix)
        {
            for (var i = 0; i < Count; i++)
            {
                if (this[i].ContainsPrefix(prefix))
                {
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc />
        public virtual ValueProviderResult GetValue(string key)
        {
            // Performance-sensitive
            // Caching the count is faster for IList<T>
            var itemCount = Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                var valueProvider = Items[i];
                var result = valueProvider.GetValue(key);
                if (result != ValueProviderResult.None)
                {
                    return result;
                }
            }

            return ValueProviderResult.None;
        }

        /// <inheritdoc />
        public virtual IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            foreach (var valueProvider in this)
            {
                if (valueProvider is IEnumerableValueProvider enumeratedProvider)
                {
                    var result = enumeratedProvider.GetKeysFromPrefix(prefix);
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                }
            }
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, IValueProvider item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, IValueProvider item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            base.SetItem(index, item);
        }

        /// <inheritdoc />
        public IValueProvider Filter(BindingSource bindingSource)
        {
            if (bindingSource == null)
            {
                throw new ArgumentNullException(nameof(bindingSource));
            }

            var shouldFilter = false;
            for (var i = 0; i < Count; i++)
            {
                var valueProvider = Items[i];
                if (valueProvider is IBindingSourceValueProvider)
                {
                    shouldFilter = true;
                    break;
                }
            }

            if (!shouldFilter)
            {
                // No inner IBindingSourceValueProvider implementations. Result will be empty.
                return null;
            }

            var filteredValueProviders = new List<IValueProvider>();
            for (var i = 0; i < Count; i++)
            {
                var valueProvider = Items[i];
                if (valueProvider is IBindingSourceValueProvider bindingSourceValueProvider)
                {
                    var result = bindingSourceValueProvider.Filter(bindingSource);
                    if (result != null)
                    {
                        filteredValueProviders.Add(result);
                    }
                }
            }

            if (filteredValueProviders.Count == 0)
            {
                // Do not create an empty CompositeValueProvider.
                return null;
            }

            return new CompositeValueProvider(filteredValueProviders);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Value providers are included by default. If a contained <see cref="IValueProvider"/> does not implement
        /// <see cref="IKeyRewriterValueProvider"/>, <see cref="Filter()"/> will not remove it.
        /// </remarks>
        public IValueProvider Filter()
        {
            var shouldFilter = false;
            for (var i = 0; i < Count; i++)
            {
                var valueProvider = Items[i];
                if (valueProvider is IKeyRewriterValueProvider)
                {
                    shouldFilter = true;
                    break;
                }
            }

            if (!shouldFilter)
            {
                // No inner IKeyRewriterValueProvider implementations. Nothing to exclude.
                return this;
            }

            var filteredValueProviders = new List<IValueProvider>();
            for (var i = 0; i < Count; i++)
            {
                var valueProvider = Items[i];
                if (valueProvider is IKeyRewriterValueProvider keyRewriterValueProvider)
                {
                    var result = keyRewriterValueProvider.Filter();
                    if (result != null)
                    {
                        filteredValueProviders.Add(result);
                    }
                }
                else
                {
                    // Assume value providers that aren't rewriter-aware do not rewrite their keys.
                    filteredValueProviders.Add(valueProvider);
                }
            }

            if (filteredValueProviders.Count == 0)
            {
                // Do not create an empty CompositeValueProvider.
                return null;
            }

            return new CompositeValueProvider(filteredValueProviders);
        }
    }
}
