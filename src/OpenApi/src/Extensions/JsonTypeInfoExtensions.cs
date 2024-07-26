// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OpenApi;

internal static class JsonTypeInfoExtensions
{
    private static readonly Dictionary<Type, string> _simpleTypeToName = new()
    {
        [typeof(bool)] = "boolean",
        [typeof(byte)] = "byte",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(DateTime)] = "DateTime",
        [typeof(DateTimeOffset)] = "DateTimeOffset",
        [typeof(Guid)] = "Guid",
        [typeof(char)] = "char",
        [typeof(Uri)] = "Uri",
        [typeof(string)] = "string",
        [typeof(IFormFile)] = "IFormFile",
        [typeof(IFormFileCollection)] = "IFormFileCollection",
        [typeof(PipeReader)] = "PipeReader",
        [typeof(Stream)] = "Stream"
    };

    /// <summary>
    /// The following method maps a JSON type to a schema reference ID that will eventually be used as the
    /// schema reference name in the OpenAPI document. These schema reference names are considered URL fragments
    /// in the context of JSON Schema's $ref keyword and must comply with the character restrictions of URL fragments.
    /// In particular, the generated strings can contain alphanumeric characters and a subset of special symbols. This
    /// means that certain symbols that appear commonly in .NET type names like ">" are not permitted in the
    /// generated reference ID.
    /// </summary>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo"/> associated with the target schema.</param>
    /// <param name="isTopLevel">
    /// When <see langword="false" />, returns schema name for primitive
    /// types to support use in list/dictionary types.
    /// </param>
    /// <returns>The schema reference ID represented as a string name.</returns>
    internal static string? GetSchemaReferenceId(this JsonTypeInfo jsonTypeInfo, bool isTopLevel = true)
    {
        var type = jsonTypeInfo.Type;
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (isTopLevel && OpenApiConstants.PrimitiveTypes.Contains(underlyingType ?? type))
        {
            return null;
        }

        // Short-hand if the type we're generating a schema reference ID for is
        // one of the simple types defined above.
        if (_simpleTypeToName.TryGetValue(type, out var simpleName))
        {
            return simpleName;
        }

        // Although arrays are enumerable types they are not encoded correctly
        // with JsonTypeInfoKind.Enumerable so we handle the Enumerble type
        // case here.
        if (jsonTypeInfo is JsonTypeInfo { Kind: JsonTypeInfoKind.Enumerable } || type.IsArray)
        {
            return null;
        }

        if (jsonTypeInfo is JsonTypeInfo { Kind: JsonTypeInfoKind.Dictionary })
        {
            return null;
        }

        return type.GetSchemaReferenceId(jsonTypeInfo.Options);
    }

    internal static string GetSchemaReferenceId(this Type type, JsonSerializerOptions options)
    {
        // Check the simple types map first to account for the element types
        // of enumerables that have been processed above.
        if (_simpleTypeToName.TryGetValue(type, out var simpleName))
        {
            return simpleName;
        }

        // Special handling for anonymous types
        if (type.Name.StartsWith("<>f", StringComparison.Ordinal))
        {
            var typeName = "AnonymousType";
            var anonymousTypeProperties = type.GetGenericArguments();
            var propertyNames = string.Join("And", anonymousTypeProperties.Select(p => p.GetSchemaReferenceId(options)));
            return $"{typeName}Of{propertyNames}";
        }

        // Special handling for generic types that are collections
        // Generic types become a concatenation of the generic type name and the type arguments
        if (type.IsGenericType)
        {
            var genericTypeName = type.Name[..type.Name.LastIndexOf('`')];
            var genericArguments = type.GetGenericArguments();
            var argumentNames = string.Join("And", genericArguments.Select(arg => arg.GetSchemaReferenceId(options)));
            return $"{genericTypeName}Of{argumentNames}";
        }
        return type.Name;
    }
}
