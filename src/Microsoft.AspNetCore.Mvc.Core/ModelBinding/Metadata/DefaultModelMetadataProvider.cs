// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="IModelMetadataProvider"/> based on reflection.
    /// </summary>
    public class DefaultModelMetadataProvider : ModelMetadataProvider
    {
        private readonly TypeCache _typeCache = new TypeCache();
        private readonly Func<ModelMetadataIdentity, ModelMetadataCacheEntry> _cacheEntryFactory;
        private readonly ModelMetadataCacheEntry _metadataCacheEntryForObjectType;

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadataProvider"/>.
        /// </summary>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        public DefaultModelMetadataProvider(ICompositeMetadataDetailsProvider detailsProvider)
            : this(detailsProvider, new DefaultModelBindingMessageProvider())
        {
        }

        /// <summary>
        /// Creates a new <see cref="DefaultModelMetadataProvider"/>.
        /// </summary>
        /// <param name="detailsProvider">The <see cref="ICompositeMetadataDetailsProvider"/>.</param>
        /// <param name="optionsAccessor">The accessor for <see cref="MvcOptions"/>.</param>
        public DefaultModelMetadataProvider(
            ICompositeMetadataDetailsProvider detailsProvider,
            IOptions<MvcOptions> optionsAccessor)
            : this(detailsProvider, GetMessageProvider(optionsAccessor))
        {
        }

        private DefaultModelMetadataProvider(
            ICompositeMetadataDetailsProvider detailsProvider,
            DefaultModelBindingMessageProvider modelBindingMessageProvider)
        {
            if (detailsProvider == null)
            {
                throw new ArgumentNullException(nameof(detailsProvider));
            }

            DetailsProvider = detailsProvider;
            ModelBindingMessageProvider = modelBindingMessageProvider;

            _cacheEntryFactory = CreateCacheEntry;
            _metadataCacheEntryForObjectType = GetMetadataCacheEntryForObjectType();
        }

        /// <summary>
        /// Gets the <see cref="ICompositeMetadataDetailsProvider"/>.
        /// </summary>
        protected ICompositeMetadataDetailsProvider DetailsProvider { get; }

        /// <summary>
        /// Gets the <see cref="Metadata.DefaultModelBindingMessageProvider"/>.
        /// </summary>
        /// <value>Same as <see cref="MvcOptions.ModelBindingMessageProvider"/> in all production scenarios.</value>
        protected DefaultModelBindingMessageProvider ModelBindingMessageProvider { get; }

        /// <inheritdoc />
        public override IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var cacheEntry = GetCacheEntry(modelType);

            // We're relying on a safe race-condition for Properties - take care only
            // to set the value onces the properties are fully-initialized.
            if (cacheEntry.Details.Properties == null)
            {
                var key = ModelMetadataIdentity.ForType(modelType);
                var propertyDetails = CreatePropertyDetails(key);

                var properties = new ModelMetadata[propertyDetails.Length];
                for (var i = 0; i < properties.Length; i++)
                {
                    propertyDetails[i].ContainerMetadata = cacheEntry.Metadata;
                    properties[i] = CreateModelMetadata(propertyDetails[i]);
                }

                cacheEntry.Details.Properties = properties;
            }

            return cacheEntry.Details.Properties;
        }

        public override ModelMetadata GetMetadataForParameter(ParameterInfo parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            var cacheEntry = GetCacheEntry(parameter);

            return cacheEntry.Metadata;
        }

        /// <inheritdoc />
        public override ModelMetadata GetMetadataForType(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            var cacheEntry = GetCacheEntry(modelType);

            return cacheEntry.Metadata;
        }

        private static DefaultModelBindingMessageProvider GetMessageProvider(IOptions<MvcOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            return optionsAccessor.Value.ModelBindingMessageProvider;
        }

        private ModelMetadataCacheEntry GetCacheEntry(Type modelType)
        {
            ModelMetadataCacheEntry cacheEntry;

            // Perf: We cached model metadata cache entry for "object" type to save ConcurrentDictionary lookups.
            if (modelType == typeof(object))
            {
                cacheEntry = _metadataCacheEntryForObjectType;
            }
            else
            {
                var key = ModelMetadataIdentity.ForType(modelType);

                cacheEntry = _typeCache.GetOrAdd(key, _cacheEntryFactory);
            }

            return cacheEntry;
        }

        private ModelMetadataCacheEntry GetCacheEntry(ParameterInfo parameter)
        {
            return _typeCache.GetOrAdd(
                ModelMetadataIdentity.ForParameter(parameter),
                _cacheEntryFactory);
        }

        private ModelMetadataCacheEntry CreateCacheEntry(ModelMetadataIdentity key)
        {
            DefaultMetadataDetails details;
            if (key.MetadataKind == ModelMetadataKind.Parameter)
            {
                details = CreateParameterDetails(key);
            }
            else
            {
                details = CreateTypeDetails(key);
            }

            var metadata = CreateModelMetadata(details);
            return new ModelMetadataCacheEntry(metadata, details);
        }

        private ModelMetadataCacheEntry GetMetadataCacheEntryForObjectType()
        {
            var key = ModelMetadataIdentity.ForType(typeof(object));
            var entry = CreateCacheEntry(key);
            return entry;
        }

        /// <summary>
        /// Creates a new <see cref="ModelMetadata"/> from a <see cref="DefaultMetadataDetails"/>.
        /// </summary>
        /// <param name="entry">The <see cref="DefaultMetadataDetails"/> entry with cached data.</param>
        /// <returns>A new <see cref="ModelMetadata"/> instance.</returns>
        /// <remarks>
        /// <see cref="DefaultModelMetadataProvider"/> will always create instances of
        /// <see cref="DefaultModelMetadata"/> .Override this method to create a <see cref="ModelMetadata"/>
        /// of a different concrete type.
        /// </remarks>
        protected virtual ModelMetadata CreateModelMetadata(DefaultMetadataDetails entry)
        {
            return new DefaultModelMetadata(this, DetailsProvider, entry, ModelBindingMessageProvider);
        }

        /// <summary>
        /// Creates the <see cref="DefaultMetadataDetails"/> entries for the properties of a model
        /// <see cref="Type"/>.
        /// </summary>
        /// <param name="key">
        /// The <see cref="ModelMetadataIdentity"/> identifying the model <see cref="Type"/>.
        /// </param>
        /// <returns>A details object for each property of the model <see cref="Type"/>.</returns>
        /// <remarks>
        /// The results of this method will be cached and used to satisfy calls to
        /// <see cref="GetMetadataForProperties(Type)"/>. Override this method to provide a different
        /// set of property data.
        /// </remarks>
        protected virtual DefaultMetadataDetails[] CreatePropertyDetails(ModelMetadataIdentity key)
        {
            var propertyHelpers = PropertyHelper.GetVisibleProperties(key.ModelType);

            var propertyEntries = new List<DefaultMetadataDetails>(propertyHelpers.Length);
            for (var i = 0; i < propertyHelpers.Length; i++)
            {
                var propertyHelper = propertyHelpers[i];
                var propertyKey = ModelMetadataIdentity.ForProperty(
                    propertyHelper.Property.PropertyType,
                    propertyHelper.Name,
                    key.ModelType);

                var attributes = ModelAttributes.GetAttributesForProperty(
                    key.ModelType,
                    propertyHelper.Property);

                var propertyEntry = new DefaultMetadataDetails(propertyKey, attributes);
                if (propertyHelper.Property.CanRead && propertyHelper.Property.GetMethod?.IsPublic == true)
                {
                    var getter = PropertyHelper.MakeNullSafeFastPropertyGetter(propertyHelper.Property);
                    propertyEntry.PropertyGetter = getter;
                }

                if (propertyHelper.Property.CanWrite &&
                    propertyHelper.Property.SetMethod?.IsPublic == true &&
                    !key.ModelType.GetTypeInfo().IsValueType)
                {
                    propertyEntry.PropertySetter = propertyHelper.ValueSetter;
                }

                propertyEntries.Add(propertyEntry);
            }

            return propertyEntries.ToArray();
        }

        /// <summary>
        /// Creates the <see cref="DefaultMetadataDetails"/> entry for a model <see cref="Type"/>.
        /// </summary>
        /// <param name="key">
        /// The <see cref="ModelMetadataIdentity"/> identifying the model <see cref="Type"/>.
        /// </param>
        /// <returns>A details object for the model <see cref="Type"/>.</returns>
        /// <remarks>
        /// The results of this method will be cached and used to satisfy calls to
        /// <see cref="GetMetadataForType(Type)"/>. Override this method to provide a different
        /// set of attributes.
        /// </remarks>
        protected virtual DefaultMetadataDetails CreateTypeDetails(ModelMetadataIdentity key)
        {
            return new DefaultMetadataDetails(
                key,
                ModelAttributes.GetAttributesForType(key.ModelType));
        }

        protected virtual DefaultMetadataDetails CreateParameterDetails(ModelMetadataIdentity key)
        {
            return new DefaultMetadataDetails(
                key,
                ModelAttributes.GetAttributesForParameter(key.ParameterInfo));
        }

        private class TypeCache : ConcurrentDictionary<ModelMetadataIdentity, ModelMetadataCacheEntry>
        {
        }

        private struct ModelMetadataCacheEntry
        {
            public ModelMetadataCacheEntry(ModelMetadata metadata, DefaultMetadataDetails details)
            {
                Metadata = metadata;
                Details = details;
            }

            public ModelMetadata Metadata { get; }

            public DefaultMetadataDetails Details { get; }
        }
    }
}