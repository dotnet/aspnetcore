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
public sealed class NewtonsoftJsonValidationMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly NamingStrategy _jsonNamingPolicy;

    /// <summary>
    /// Creates a new <see cref="NewtonsoftJsonValidationMetadataProvider"/> with the default <see cref="CamelCaseNamingStrategy"/>
    /// </summary>
    public NewtonsoftJsonValidationMetadataProvider()
        : this(new CamelCaseNamingStrategy())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="NewtonsoftJsonValidationMetadataProvider"/> with an optional <see cref="NamingStrategy"/>
    /// </summary>
    /// <param name="namingStrategy">The <see cref="NamingStrategy"/> to be used to configure the metadata provider.</param>
    public NewtonsoftJsonValidationMetadataProvider(NamingStrategy namingStrategy)
    {
        ArgumentNullException.ThrowIfNull(namingStrategy);

        _jsonNamingPolicy = namingStrategy;
    }

    /// <inheritdoc />
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var propertyName = ReadPropertyNameFrom(context.Attributes);

        if (!string.IsNullOrEmpty(propertyName))
        {
            context.DisplayMetadata.DisplayName = () => propertyName;
        }
    }

    /// <inheritdoc />
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
