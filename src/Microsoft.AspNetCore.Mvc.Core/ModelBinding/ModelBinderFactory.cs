// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A factory for <see cref="IModelBinder"/> instances.
    /// </summary>
    public class ModelBinderFactory : IModelBinderFactory
    {
        private readonly IModelMetadataProvider _metadataProvider;
        private readonly IModelBinderProvider[] _providers;

        private readonly ConcurrentDictionary<object, IModelBinder> _cache;

        /// <summary>
        /// Creates a new <see cref="ModelBinderFactory"/>.
        /// </summary>
        /// <param name="metadataProvider">The <see cref="IModelMetadataProvider"/>.</param>
        /// <param name="options">The <see cref="IOptions{TOptions}"/> for <see cref="MvcOptions"/>.</param>
        public ModelBinderFactory(IModelMetadataProvider metadataProvider, IOptions<MvcOptions> options)
        {
            _metadataProvider = metadataProvider;
            _providers = options.Value.ModelBinderProviders.ToArray();

            _cache = new ConcurrentDictionary<object, IModelBinder>(ReferenceEqualityComparer.Instance);
        }

        /// <inheritdoc />
        public IModelBinder CreateBinder(ModelBinderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // We perform caching in CreateBinder (not in CreateBinderCore) because we only want to
            // cache the top-level binder.
            IModelBinder binder;
            if (context.CacheToken != null && _cache.TryGetValue(context.CacheToken, out binder))
            {
                return binder;
            }

            var providerContext = new DefaultModelBinderProviderContext(this, context);
            binder = CreateBinderCore(providerContext, context.CacheToken);
            if (binder == null)
            {
                var message = Resources.FormatCouldNotCreateIModelBinder(providerContext.Metadata.ModelType);
                throw new InvalidOperationException(message);
            }

            if (context.CacheToken != null)
            {
                _cache.TryAdd(context.CacheToken, binder);
            }

            return binder;
        }

        private IModelBinder CreateBinderCore(DefaultModelBinderProviderContext providerContext, object token)
        {
            if (!providerContext.Metadata.IsBindingAllowed)
            {
                return NoOpBinder.Instance;
            }

            // A non-null token will usually be passed in at the the top level (ParameterDescriptor likely).
            // This prevents us from treating a parameter the same as a collection-element - which could
            // happen looking at just model metadata.
            var key = new Key(providerContext.Metadata, token);

            // If we're currently recursively building a binder for this type, just return
            // a PlaceholderBinder. We'll fix it up later to point to the 'real' binder
            // when the stack unwinds.
            var stack = providerContext.Stack;
            for (var i = 0; i < stack.Count; i++)
            {
                var entry = stack[i];
                if (key.Equals(entry.Key))
                {
                    if (entry.Value == null)
                    {
                        // Recursion detected, create a DelegatingBinder.
                        var binder = new PlaceholderBinder();
                        stack[i] = new KeyValuePair<Key, PlaceholderBinder>(entry.Key, binder);
                        return binder;
                    }
                    else
                    {
                        return entry.Value;
                    }
                }
            }

            // OK this isn't a recursive case (yet) so "push" an entry on the stack and then ask the providers
            // to create the binder.
            stack.Add(new KeyValuePair<Key, PlaceholderBinder>(key, null));

            IModelBinder result = null;

            for (var i = 0; i < _providers.Length; i++)
            {
                var provider = _providers[i];
                result = provider.GetBinder(providerContext);
                if (result != null)
                {
                    break;
                }
            }

            if (result == null && stack.Count > 1)
            {
                // Use a no-op binder if we're below the top level. At the top level, we throw.
                result = NoOpBinder.Instance;
            }

            // "pop"
            Debug.Assert(stack.Count > 0);
            var delegatingBinder = stack[stack.Count - 1].Value;
            stack.RemoveAt(stack.Count - 1);

            // If the DelegatingBinder was created, then it means we recursed. Hook it up to the 'real' binder.
            if (delegatingBinder != null)
            {
                delegatingBinder.Inner = result;
            }

            return result;
        }

        private class DefaultModelBinderProviderContext : ModelBinderProviderContext
        {
            private readonly ModelBinderFactory _factory;

            public DefaultModelBinderProviderContext(
                ModelBinderFactory factory,
                ModelBinderFactoryContext factoryContext)
            {
                _factory = factory;
                Metadata = factoryContext.Metadata;
                BindingInfo = new BindingInfo
                {
                    BinderModelName = factoryContext.BindingInfo?.BinderModelName ?? Metadata.BinderModelName,
                    BinderType = factoryContext.BindingInfo?.BinderType ?? Metadata.BinderType,
                    BindingSource = factoryContext.BindingInfo?.BindingSource ?? Metadata.BindingSource,
                    PropertyFilterProvider =
                        factoryContext.BindingInfo?.PropertyFilterProvider ?? Metadata.PropertyFilterProvider,
                };

                MetadataProvider = _factory._metadataProvider;
                Stack = new List<KeyValuePair<Key, PlaceholderBinder>>();
            }

            private DefaultModelBinderProviderContext(
                DefaultModelBinderProviderContext parent,
                ModelMetadata metadata)
            {
                Metadata = metadata;

                _factory = parent._factory;
                MetadataProvider = parent.MetadataProvider;
                Stack = parent.Stack;

                BindingInfo = new BindingInfo()
                {
                    BinderModelName = metadata.BinderModelName,
                    BinderType = metadata.BinderType,
                    BindingSource = metadata.BindingSource,
                    PropertyFilterProvider = metadata.PropertyFilterProvider,
                };
            }

            public override BindingInfo BindingInfo { get; }

            public override ModelMetadata Metadata { get; }

            public override IModelMetadataProvider MetadataProvider { get; }

            // Not using a 'real' Stack<> because we want random access to modify the entries.
            public List<KeyValuePair<Key, PlaceholderBinder>> Stack { get; }

            public override IModelBinder CreateBinder(ModelMetadata metadata)
            {
                var nestedContext = new DefaultModelBinderProviderContext(this, metadata);
                return _factory.CreateBinderCore(nestedContext, token: null);
            }
        }

        private struct Key : IEquatable<Key>
        {
            private readonly ModelMetadata _metadata;
            private readonly object _token; // Explicitly using ReferenceEquality for tokens.

            public Key(ModelMetadata metadata, object token)
            {
                _metadata = metadata;
                _token = token;
            }

            public bool Equals(Key other)
            {
                return _metadata.Equals(other._metadata) && object.ReferenceEquals(_token, other._token);
            }

            public override bool Equals(object obj)
            {
                var other = obj as Key?;
                return other.HasValue && Equals(other.Value);
            }

            public override int GetHashCode()
            {
                var hash = new HashCodeCombiner();
                hash.Add(_metadata);
                hash.Add(RuntimeHelpers.GetHashCode(_token));
                return hash;
            }
        }
    }
}
