// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// A default implementation of <see cref="IModelMetadataProvider"/> based on reflection.
/// </summary>
public class DefaultModelMetadataProvider : ModelMetadataProvider
{
    private readonly ConcurrentDictionary<ModelMetadataIdentity, ModelMetadataCacheEntry> _modelMetadataCache = new();
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
        ArgumentNullException.ThrowIfNull(detailsProvider);

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

    internal void ClearCache() => _modelMetadataCache.Clear();

    /// <inheritdoc />
    public override IEnumerable<ModelMetadata> GetMetadataForProperties(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var cacheEntry = GetCacheEntry(modelType);

        // We're relying on a safe race-condition for Properties - take care only
        // to set the value once the properties are fully-initialized.
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

    /// <inheritdoc />
    public override ModelMetadata GetMetadataForParameter(ParameterInfo parameter)
        => GetMetadataForParameter(parameter, parameter.ParameterType);

    /// <inheritdoc />
    public override ModelMetadata GetMetadataForParameter(ParameterInfo parameter, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentNullException.ThrowIfNull(modelType);

        var cacheEntry = GetCacheEntry(parameter, modelType);

        return cacheEntry.Metadata;
    }

    /// <inheritdoc />
    public override ModelMetadata GetMetadataForType(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var cacheEntry = GetCacheEntry(modelType);

        return cacheEntry.Metadata;
    }

    /// <inheritdoc />
    public override ModelMetadata GetMetadataForProperty(PropertyInfo propertyInfo, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        ArgumentNullException.ThrowIfNull(modelType);

        var cacheEntry = GetCacheEntry(propertyInfo, modelType);

        return cacheEntry.Metadata;
    }

    /// <inheritdoc />
    public override ModelMetadata GetMetadataForConstructor(ConstructorInfo constructorInfo, Type modelType)
    {
        ArgumentNullException.ThrowIfNull(constructorInfo);

        var cacheEntry = GetCacheEntry(constructorInfo, modelType);
        return cacheEntry.Metadata;
    }

    private static DefaultModelBindingMessageProvider GetMessageProvider(IOptions<MvcOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);

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

            cacheEntry = _modelMetadataCache.GetOrAdd(key, _cacheEntryFactory);
        }

        return cacheEntry;
    }

    private ModelMetadataCacheEntry GetCacheEntry(ParameterInfo parameter, Type modelType)
    {
        return _modelMetadataCache.GetOrAdd(
            ModelMetadataIdentity.ForParameter(parameter, modelType),
            _cacheEntryFactory);
    }

    private ModelMetadataCacheEntry GetCacheEntry(PropertyInfo property, Type modelType)
    {
        return _modelMetadataCache.GetOrAdd(
            ModelMetadataIdentity.ForProperty(property, modelType, property.DeclaringType!),
            _cacheEntryFactory);
    }

    private ModelMetadataCacheEntry GetCacheEntry(ConstructorInfo constructor, Type modelType)
    {
        return _modelMetadataCache.GetOrAdd(
            ModelMetadataIdentity.ForConstructor(constructor, modelType),
            _cacheEntryFactory);
    }

    private ModelMetadataCacheEntry CreateCacheEntry(ModelMetadataIdentity key)
    {
        DefaultMetadataDetails details;

        if (key.MetadataKind == ModelMetadataKind.Constructor)
        {
            details = CreateConstructorDetails(key);
        }
        else if (key.MetadataKind == ModelMetadataKind.Parameter)
        {
            details = CreateParameterDetails(key);
        }
        else if (key.MetadataKind == ModelMetadataKind.Property)
        {
            details = CreateSinglePropertyDetails(key);
        }
        else
        {
            details = CreateTypeDetails(key);
        }

        var metadata = CreateModelMetadata(details);
        return new ModelMetadataCacheEntry(metadata, details);
    }

    private static DefaultMetadataDetails CreateSinglePropertyDetails(ModelMetadataIdentity propertyKey)
    {
        var propertyHelpers = PropertyHelper.GetVisibleProperties(propertyKey.ContainerType!);
        for (var i = 0; i < propertyHelpers.Length; i++)
        {
            var propertyHelper = propertyHelpers[i];
            if (propertyHelper.Name == propertyKey.Name)
            {
                return CreateSinglePropertyDetails(propertyKey, propertyHelper);
            }
        }

        Debug.Fail($"Unable to find property '{propertyKey.Name}' on type '{propertyKey.ContainerType}.");
        return null;
    }

    private DefaultMetadataDetails CreateConstructorDetails(ModelMetadataIdentity constructorKey)
    {
        var constructor = constructorKey.ConstructorInfo;
        var parameters = constructor!.GetParameters();
        var parameterMetadata = new ModelMetadata[parameters.Length];
        var parameterTypes = new Type[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var parameterDetails = CreateParameterDetails(ModelMetadataIdentity.ForParameter(parameter));
            parameterMetadata[i] = CreateModelMetadata(parameterDetails);

            parameterTypes[i] = parameter.ParameterType;
        }

        var constructorDetails = new DefaultMetadataDetails(constructorKey, ModelAttributes.Empty);
        constructorDetails.BoundConstructorParameters = parameterMetadata;
        constructorDetails.BoundConstructorInvoker = CreateObjectFactory(constructor);

        return constructorDetails;

        static Func<object?[], object> CreateObjectFactory(ConstructorInfo constructor)
        {
            var args = Expression.Parameter(typeof(object?[]), "args");
            var factoryExpressionBody = BuildFactoryExpression(constructor, args);

            var factoryLamda = Expression.Lambda<Func<object?[], object>>(factoryExpressionBody, args);

            return factoryLamda.Compile();
        }
    }

    private static Expression BuildFactoryExpression(
        ConstructorInfo constructor,
        Expression factoryArgumentArray)
    {
        var constructorParameters = constructor.GetParameters();
        var constructorArguments = new Expression[constructorParameters.Length];

        for (var i = 0; i < constructorParameters.Length; i++)
        {
            var constructorParameter = constructorParameters[i];
            var parameterType = constructorParameter.ParameterType;

            constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(i));
            if (ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out var defaultValue))
            {
                // We have a default value;
            }
            else if (parameterType.IsValueType)
            {
                defaultValue = Activator.CreateInstance(parameterType);
            }

            if (defaultValue != null)
            {
                var defaultValueExpression = Expression.Constant(defaultValue);
                constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
            }

            constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
        }

        return Expression.New(constructor, constructorArguments);
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
                propertyHelper.Property,
                propertyHelper.Property.PropertyType,
                key.ModelType);

            var propertyEntry = CreateSinglePropertyDetails(propertyKey, propertyHelper);
            propertyEntries.Add(propertyEntry);
        }

        return propertyEntries.ToArray();
    }

    private static DefaultMetadataDetails CreateSinglePropertyDetails(
        ModelMetadataIdentity propertyKey,
        PropertyHelper propertyHelper)
    {
        Debug.Assert(propertyKey.MetadataKind == ModelMetadataKind.Property);
        var containerType = propertyKey.ContainerType!;

        var attributes = ModelAttributes.GetAttributesForProperty(
            containerType,
            propertyHelper.Property,
            propertyKey.ModelType);

        var propertyEntry = new DefaultMetadataDetails(propertyKey, attributes);
        if (propertyHelper.Property.CanRead && propertyHelper.Property.GetMethod?.IsPublic == true)
        {
            var getter = PropertyHelper.MakeNullSafeFastPropertyGetter(propertyHelper.Property);
            propertyEntry.PropertyGetter = getter;
        }

        if (propertyHelper.Property.CanWrite &&
            propertyHelper.Property.SetMethod?.IsPublic == true &&
            !containerType.IsValueType)
        {
            propertyEntry.PropertySetter = propertyHelper.ValueSetter;
        }

        return propertyEntry;
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

    /// <summary>
    /// Creates the <see cref="DefaultMetadataDetails"/> entry for a parameter <see cref="Type"/>.
    /// </summary>
    /// <param name="key">
    /// The <see cref="ModelMetadataIdentity"/> identifying the parameter <see cref="Type"/>.
    /// </param>
    /// <returns>A details object for the parameter.</returns>
    protected virtual DefaultMetadataDetails CreateParameterDetails(ModelMetadataIdentity key)
    {
        return new DefaultMetadataDetails(
            key,
            ModelAttributes.GetAttributesForParameter(key.ParameterInfo!, key.ModelType));
    }

    private readonly struct ModelMetadataCacheEntry
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
