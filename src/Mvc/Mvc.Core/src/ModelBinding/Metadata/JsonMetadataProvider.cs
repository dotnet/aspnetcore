// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

/// <summary>
/// An implementation of <see cref="IDisplayMetadataProvider"/> and <see cref="IValidationMetadataProvider"/> for
/// the System.Text.Json.Serialization attribute classes.
/// </summary>
public class JsonMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly JsonNamingPolicy _jsonNamingPolicy = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// Creates a new <see cref="JsonMetadataProvider"/> with the default <see cref="JsonNamingPolicy.CamelCase"/>
    /// </summary>
    public JsonMetadataProvider()
    { }

    /// <summary>
    /// Creates a new <see cref="JsonMetadataProvider"/> with an optional <see cref="JsonOptions"/>
    /// </summary>
    /// <param name="jsonOptions">The <see cref="JsonOptions"/> to be used to configure the metadata provider.</param>
    public JsonMetadataProvider(JsonOptions jsonOptions)
    {
        if (jsonOptions == null)
        {
            throw new ArgumentNullException(nameof(jsonOptions));
        }

        if (jsonOptions.JsonSerializerOptions?.PropertyNamingPolicy != null)
        {
            _jsonNamingPolicy = jsonOptions.JsonSerializerOptions.PropertyNamingPolicy;
        }
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
            propertyName = _jsonNamingPolicy.ConvertName(context.Key.Name!);
        }

        context.ValidationMetadata.ValidationModelName = propertyName;
    }

    private static string? ReadPropertyNameFrom(IReadOnlyList<object> attributes)
        => attributes?.OfType<JsonPropertyNameAttribute>().FirstOrDefault()?.Name;
}
