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
internal class JsonMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly JsonNamingPolicy? _jsonNamingPolicy;

    /// <summary>
    /// Creates a new <see cref="JsonMetadataProvider"/>.
    /// </summary>
    public JsonMetadataProvider()
        : this(JsonNamingPolicy.CamelCase)
    {
    }

    /// <summary>
    /// Creates a new <see cref="JsonMetadataProvider"/> with an optional <see cref="JsonNamingPolicy"/>
    /// </summary>
    /// <param name="jsonNamingPolicy">The <see cref="JsonNamingPolicy"/> to be used to convert the property name</param>
    public JsonMetadataProvider(JsonNamingPolicy? jsonNamingPolicy)
        => _jsonNamingPolicy = jsonNamingPolicy;

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
            propertyName = _jsonNamingPolicy?.ConvertName(context.Key.Name!) ?? context.Key.Name;
        }

        context.ValidationMetadata.ValidationModelName = propertyName;
    }

    private static string? ReadPropertyNameFrom(IReadOnlyList<object> attributes)
        => attributes?.OfType<JsonPropertyNameAttribute>().FirstOrDefault()?.Name;
}
