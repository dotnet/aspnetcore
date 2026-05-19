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
public sealed class SystemTextJsonValidationMetadataProvider : IDisplayMetadataProvider, IValidationMetadataProvider
{
    private readonly JsonNamingPolicy _jsonNamingPolicy;

    /// <summary>
    /// Creates a new <see cref="SystemTextJsonValidationMetadataProvider"/> with the default <see cref="JsonNamingPolicy.CamelCase"/>
    /// </summary>
    public SystemTextJsonValidationMetadataProvider()
        : this(JsonNamingPolicy.CamelCase)
    { }

    /// <summary>
    /// Creates a new <see cref="SystemTextJsonValidationMetadataProvider"/> with an optional <see cref="JsonNamingPolicy"/>
    /// </summary>
    /// <param name="namingPolicy">The <see cref="JsonNamingPolicy"/> to be used to configure the metadata provider.</param>
    public SystemTextJsonValidationMetadataProvider(JsonNamingPolicy namingPolicy)
    {
        ArgumentNullException.ThrowIfNull(namingPolicy);

        _jsonNamingPolicy = namingPolicy;
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
            propertyName = context.Key.Name is string contextKeyName
                ? _jsonNamingPolicy.ConvertName(contextKeyName)
                : null;
        }

        context.ValidationMetadata.ValidationModelName = propertyName;
    }

    private static string? ReadPropertyNameFrom(IReadOnlyList<object> attributes)
        => attributes?.OfType<JsonPropertyNameAttribute>().FirstOrDefault()?.Name;
}
