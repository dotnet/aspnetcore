// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

/// <summary>
/// A JsonConverter for System.Type that serializes types as assembly name and type name,
/// and deserializes by loading the type from the appropriate assembly.
/// This converter maintains a cache to avoid repeated lookups.
/// </summary>
internal sealed class TypeJsonConverter : JsonConverter<Type>
{
    private static readonly ConcurrentDictionary<TypeKey, Type?> _typeCache = new();

    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject, got {reader.TokenType}");
        }

        string? assemblyName = null;
        string? typeName = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "assembly":
                        assemblyName = reader.GetString();
                        break;
                    case "type":
                        typeName = reader.GetString();
                        break;
                    default:
                        throw new JsonException($"Unexpected property '{propertyName}' in Type JSON.");
                }
            }
        }

        if (string.IsNullOrEmpty(assemblyName) || string.IsNullOrEmpty(typeName))
        {
            throw new JsonException("Type JSON must contain both 'assembly' and 'type' properties.");
        }

        var typeKey = new TypeKey(assemblyName, typeName);
        return _typeCache.GetOrAdd(typeKey, ResolveType);
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        var assemblyName = value.Assembly.GetName().Name;
        if (string.IsNullOrEmpty(assemblyName))
        {
            throw new InvalidOperationException("Cannot serialize type from assembly with null name.");
        }

        var typeName = value.FullName;
        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException("Cannot serialize type with null FullName.");
        }

        writer.WriteStartObject();
        writer.WriteString("assembly", assemblyName);
        writer.WriteString("type", typeName);
        writer.WriteEndObject();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Types used with JSInterop are expected to be in assemblies that don't get trimmed.")]
    private static Type? ResolveType(TypeKey typeKey)
    {
        // First try to find the assembly among already loaded assemblies
        Assembly? assembly = null;
        foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (loadedAssembly.GetName().Name == typeKey.AssemblyName)
            {
                assembly = loadedAssembly;
                break;
            }
        }

        if (assembly is null)
        {
            // If assembly is not loaded yet, try to load it by name
            try
            {
                assembly = Assembly.Load(typeKey.AssemblyName);
            }
            catch
            {
                throw new InvalidOperationException($"Cannot load assembly '{typeKey.AssemblyName}' for type deserialization.");
            }
        }

        var type = assembly.GetType(typeKey.TypeName);
        if (type is null)
        {
            throw new InvalidOperationException($"Cannot find type '{typeKey.TypeName}' in assembly '{typeKey.AssemblyName}'.");
        }

        return type;
    }

    internal static void ClearCache()
    {
        _typeCache.Clear();
    }

    private readonly struct TypeKey : IEquatable<TypeKey>
    {
        public TypeKey(string assemblyName, string typeName)
        {
            AssemblyName = assemblyName;
            TypeName = typeName;
        }

        public string AssemblyName { get; }
        public string TypeName { get; }

        public bool Equals(TypeKey other)
        {
            return AssemblyName.Equals(other.AssemblyName, StringComparison.Ordinal) &&
                   TypeName.Equals(other.TypeName, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(AssemblyName),
                StringComparer.Ordinal.GetHashCode(TypeName));
        }
    }
}