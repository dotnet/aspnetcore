// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// An implementation of <see cref="IDisplayMetadataProvider"/> and <see cref="IValidationMetadataProvider"/> for
/// the Newtonsoft.Json attribute classes.
/// </summary>
public class NewtonsoftJsonMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly NamingStrategy _jsonNamingPolicy;

    /// <summary>
    /// Creates a new <see cref="JsonMetadataProvider"/> with the default <see cref="CamelCaseNamingStrategy"/>
    /// </summary>
    public NewtonsoftJsonMetadataProvider()
        : this(new CamelCaseNamingStrategy())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="NewtonsoftJsonMetadataProvider"/> with an optional <see cref="NamingStrategy"/>
    /// </summary>
    /// <param name="namingStrategy">The <see cref="NamingStrategy"/> to be used to configure the metadata provider.</param>
    public NewtonsoftJsonMetadataProvider(NamingStrategy namingStrategy)
    {
        if (namingStrategy == null)
        {
            throw new ArgumentNullException(nameof(namingStrategy));
        }

        _jsonNamingPolicy = namingStrategy;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="NewtonsoftJsonMetadataProvider"/> with an optional <see cref="MvcNewtonsoftJsonOptions"/>
    /// </summary>
    /// <param name="jsonOptions">The <see cref="MvcNewtonsoftJsonOptions"/> to be used to configure the metadata provider.</param>
    internal NewtonsoftJsonMetadataProvider(MvcNewtonsoftJsonOptions jsonOptions)
    {
        if (jsonOptions == null)
        {
            throw new ArgumentNullException(nameof(jsonOptions));
        }

        var contractResolver = jsonOptions.SerializerSettings?.ContractResolver as DefaultContractResolver;
        _jsonNamingPolicy = contractResolver?.NamingStrategy != null ? contractResolver.NamingStrategy : new CamelCaseNamingStrategy();
    }

    /// <inheritdoc />
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var propertyName = ReadPropertyNameFrom(context.Attributes);

        if (!string.IsNullOrEmpty(propertyName))
        {
            context.DisplayMetadata.DisplayName = () => propertyName;
        }
    }

    /// <inheritdoc />
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var propertyName = ReadPropertyNameFrom(context.Attributes);

        if (string.IsNullOrEmpty(propertyName))
        {
            propertyName = _jsonNamingPolicy.GetPropertyName(context.Key.Name!, false);
        }

        context.ValidationMetadata.ValidationModelName = propertyName!;
    }

    private static string? ReadPropertyNameFrom(IReadOnlyList<object> attributes)
        => attributes?.OfType<JsonPropertyAttribute>().FirstOrDefault()?.PropertyName;
}
