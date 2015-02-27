// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="IModelMetadataProvider"/> based on reflection.
    /// </summary>
    public class DefaultModelMetadataProvider : IModelMetadataProvider
    {
        private readonly TypeCache _typeCache = new TypeCache();
        private readonly ParameterCache _parameterCache = new ParameterCache();
        private readonly PropertiesCache _propertiesCache = new PropertiesCache();

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadataProvider"/>.
        /// </summary>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        public DefaultModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider)
        {
            DetailsProvider = detailsProvider;
        }

        /// <summary>
        /// Gets the <see cref="ICompositeMetadataDetailsProvider"/>.
        /// </summary>
        protected ICompositeMetadataDetailsProvider DetailsProvider { get; }

        /// <inheritdoc />
        public virtual ModelMetadata GetMetadataForParameter(
            [NotNull] ParameterInfo parameterInfo,
            [NotNull] IEnumerable<object> attributes)
        {
            var key = ModelMetadataIdentity.ForParameter(parameterInfo);

            DefaultMetadataDetailsCache entry;
            if (!_parameterCache.TryGetValue(key, out entry))
            {
                entry = CreateParameterCacheEntry(key, attributes);
                entry = _parameterCache.GetOrAdd(key, entry);
            }

            return CreateModelMetadata(entry);
        }

        /// <inheritdoc />
        public virtual IEnumerable<ModelMetadata> GetMetadataForProperties([NotNull]Type modelType)
        {
            var key = ModelMetadataIdentity.ForType(modelType);

            var propertyEntries = _propertiesCache.GetOrAdd(key, CreatePropertyCacheEntries);

            var properties = new ModelMetadata[propertyEntries.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                properties[i] = CreateModelMetadata(propertyEntries[i]);
            }

            return properties;
        }

        /// <inheritdoc />
        public virtual ModelMetadata GetMetadataForType([NotNull] Type modelType)
        {
            var key = ModelMetadataIdentity.ForType(modelType);

            var entry = _typeCache.GetOrAdd(key, CreateTypeCacheEntry);
            return CreateModelMetadata(entry);
        }

        /// <summary>
        /// Creates a new <see cref="ModelMetadata"/> from a <see cref="DefaultMetadataDetailsCache"/>.
        /// </summary>
        /// <param name="entry">The <see cref="DefaultMetadataDetailsCache"/> entry with cached data.</param>
        /// <returns>A new <see cref="ModelMetadata"/> instance.</returns>
        /// <remarks>
        /// <see cref="DefaultModelMetadataProvider"/> will always create instances of
        /// <see cref="DefaultModelMetadata"/> .Override this method to create a <see cref="ModelMetadata"/>
        /// of a different concrete type.
        /// </remarks>
        protected virtual ModelMetadata CreateModelMetadata(DefaultMetadataDetailsCache entry)
        {
            return new DefaultModelMetadata(this, DetailsProvider, entry);
        }

        /// <summary>
        /// Creates the <see cref="DefaultMetadataDetailsCache"/> entries for the properties of a model
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="key">
        /// The <see cref="ModelMetadataIdentity"/> identifying the model <see cref="Type"/>.
        /// </param>
        /// <returns>A cache object for each property of the model <see cref="Type"/>.</returns>
        /// <remarks>
        /// The results of this method will be cached and used to satisfy calls to
        /// <see cref="GetMetadataForProperties(Type)"/>. Override this method to provide a different
        /// set of property data.
        /// </remarks>
        protected virtual DefaultMetadataDetailsCache[] CreatePropertyCacheEntries([NotNull] ModelMetadataIdentity key)
        {
            var propertyHelpers = PropertyHelper.GetProperties(key.ModelType);

            var propertyEntries = new DefaultMetadataDetailsCache[propertyHelpers.Length];
            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                var propertyHelper = propertyHelpers[i];
                var propertyKey = ModelMetadataIdentity.ForProperty(
                    propertyHelper.Property.PropertyType,
                    propertyHelper.Name,
                    key.ModelType);

                var attributes = new List<object>(ModelAttributes.GetAttributesForProperty(
                    key.ModelType, 
                    propertyHelper.Property));

                propertyEntries[i] = new DefaultMetadataDetailsCache(propertyKey, attributes);
                if (propertyHelper.Property.CanRead && propertyHelper.Property.GetMethod?.IsPrivate == true)
                {
                    propertyEntries[i].PropertyAccessor = PropertyHelper.MakeFastPropertyGetter(propertyHelper.Property);
                }

                if (propertyHelper.Property.CanWrite && propertyHelper.Property.SetMethod?.IsPrivate == true)
                {
                    propertyEntries[i].PropertySetter = PropertyHelper.MakeFastPropertySetter(propertyHelper.Property);
                }
            }

            return propertyEntries;
        }

        /// <summary>
        /// Creates the <see cref="DefaultMetadataDetailsCache"/> entry for a model <see cref="Type"/>.
        /// </summary>
        /// <param name="key">
        /// The <see cref="ModelMetadataIdentity"/> identifying the model <see cref="Type"/>.
        /// </param>
        /// <returns>A cache object for the model <see cref="Type"/>.</returns>
        /// <remarks>
        /// The results of this method will be cached and used to satisfy calls to
        /// <see cref="GetMetadataForType(Type)"/>. Override this method to provide a different
        /// set of attributes.
        /// </remarks>
        protected virtual DefaultMetadataDetailsCache CreateTypeCacheEntry([NotNull] ModelMetadataIdentity key)
        {
            var attributes = new List<object>(ModelAttributes.GetAttributesForType(key.ModelType));
            return new DefaultMetadataDetailsCache(key, attributes);
        }

        /// <summary>
        /// Creates the <see cref="DefaultMetadataDetailsCache"/> entry for a parameter.
        /// </summary>
        /// <param name="key">
        /// The <see cref="ModelMetadataIdentity"/> identifying the model parameter.
        /// </param>
        /// <param name="attributes">
        /// The set of model attributes applied to the parameter.
        /// </param>
        /// <returns>A cache object for the model parameter.</returns>
        /// <remarks>
        /// The results of this method will be cached and used to satisfy calls to
        /// <see cref="GetMetadataForParameter(ParameterInfo, IEnumerable{object}))"/>.
        /// Override this method to provide a different set of attributes.
        /// </remarks>
        protected virtual DefaultMetadataDetailsCache CreateParameterCacheEntry(
            [NotNull] ModelMetadataIdentity key,
            [NotNull] IEnumerable<object> attributes)
        {
            var allAttributes = new List<object>();

            if (attributes != null)
            {
                allAttributes.AddRange(attributes);
            }

            allAttributes.AddRange(ModelAttributes.GetAttributesForParameter(key.ParameterInfo));

            return new DefaultMetadataDetailsCache(key, allAttributes);
        }

        private class TypeCache : ConcurrentDictionary<ModelMetadataIdentity, DefaultMetadataDetailsCache>
        {
        }

        private class PropertiesCache : ConcurrentDictionary<ModelMetadataIdentity, DefaultMetadataDetailsCache[]>
        {
        }

        private class ParameterCache : ConcurrentDictionary<ModelMetadataIdentity, DefaultMetadataDetailsCache>
        {
        }
    }
}