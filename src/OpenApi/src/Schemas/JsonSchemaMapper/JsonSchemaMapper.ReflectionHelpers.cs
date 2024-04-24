// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JsonSchemaMapper;

#if EXPOSE_JSON_SCHEMA_MAPPER
public
#else
internal
#endif
    static partial class JsonSchemaMapper
{
    // Uses reflection to determine the element type of an enumerable or dictionary type
    // Workaround for https://github.com/dotnet/runtime/issues/77306#issuecomment-2007887560
    private static Type GetElementType(JsonTypeInfo typeInfo)
    {
        Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary);
        return (Type)typeof(JsonTypeInfo).GetProperty("ElementType", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(typeInfo)!;
    }

    // The source generator currently doesn't populate attribute providers for properties
    // cf. https://github.com/dotnet/runtime/issues/100095
    // Work around the issue by running a query for the relevant MemberInfo using the internal MemberName property
    // https://github.com/dotnet/runtime/blob/de774ff9ee1a2c06663ab35be34b755cd8d29731/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonPropertyInfo.cs#L206
#if NETCOREAPP
    [EditorBrowsable(EditorBrowsableState.Never)]
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "We're reading the internal JsonPropertyInfo.MemberName which cannot have been trimmed away.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
        Justification = "We're reading the member which is already accessed by the source generator.")]
#endif
    internal static ICustomAttributeProvider? ResolveAttributeProvider(Type? declaringType, JsonPropertyInfo? propertyInfo)
    {
        if (declaringType is null || propertyInfo is null)
        {
            return null;
        }

        if (propertyInfo.AttributeProvider is { } provider)
        {
            return provider;
        }

        s_memberNameProperty ??= typeof(JsonPropertyInfo).GetProperty("MemberName", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var memberName = (string?)s_memberNameProperty.GetValue(propertyInfo);
        if (memberName is not null)
        {
            return declaringType.GetMember(memberName, MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault();
        }

        return null;
    }

    private static PropertyInfo? s_memberNameProperty;

    // Uses reflection to determine any custom converters specified for the element of a nullable type.
#if NETCOREAPP
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "We're resolving private fields of the built-in Nullable converter which cannot have been trimmed away.")]
#endif
    private static JsonConverter? ExtractCustomNullableConverter(JsonConverter? converter)
    {
        Debug.Assert(converter is null || IsBuiltInConverter(converter));

        // There is unfortunately no way in which we can obtain the element converter from a nullable converter without resorting to private reflection
        // https://github.com/dotnet/runtime/blob/5fda47434cecc590095e9aef3c4e560b7b7ebb47/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/NullableConverter.cs#L15-L17
        Type? converterType = converter?.GetType();
        if (converterType?.Name == "NullableConverter`1")
        {
            FieldInfo elementConverterField = converterType.GetPrivateFieldWithPotentiallyTrimmedMetadata("_elementConverter");
            return (JsonConverter)elementConverterField!.GetValue(converter)!;
        }

        return null;
    }

    // Uses reflection to determine serialization configuration for enum types
    // cf. https://github.com/dotnet/runtime/blob/5fda47434cecc590095e9aef3c4e560b7b7ebb47/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/EnumConverter.cs#L23-L25
#if NETCOREAPP
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "We're resolving private fields of the built-in enum converter which cannot have been trimmed away.")]
#endif
    private static bool TryGetStringEnumConverterValues(JsonTypeInfo typeInfo, JsonConverter converter, out JsonArray? values)
    {
        Debug.Assert(typeInfo.Type.IsEnum && IsBuiltInConverter(converter));

        if (converter is JsonConverterFactory factory)
        {
            converter = factory.CreateConverter(typeInfo.Type, typeInfo.Options)!;
        }

        Type converterType = converter.GetType();
        FieldInfo converterOptionsField = converterType.GetPrivateFieldWithPotentiallyTrimmedMetadata("_converterOptions");
        FieldInfo namingPolicyField = converterType.GetPrivateFieldWithPotentiallyTrimmedMetadata("_namingPolicy");

        const int EnumConverterOptionsAllowStrings = 1;
        var converterOptions = (int)converterOptionsField!.GetValue(converter)!;
        if ((converterOptions & EnumConverterOptionsAllowStrings) != 0)
        {
            if (typeInfo.Type.GetCustomAttribute<FlagsAttribute>() is not null)
            {
                // For enums implemented as flags do not surface values in the JSON schema.
                values = null;
            }
            else
            {
                var namingPolicy = (JsonNamingPolicy?)namingPolicyField!.GetValue(converter)!;
                string[] names = Enum.GetNames(typeInfo.Type);
                values = new JsonArray();
                foreach (string name in names)
                {
                    string effectiveName = namingPolicy?.ConvertName(name) ?? name;
                    values.Add((JsonNode)effectiveName);
                }
            }

            return true;
        }

        values = null;
        return false;
    }

#if NETCOREAPP
    [RequiresUnreferencedCode("Resolves unreferenced member metadata.")]
#endif
    private static FieldInfo GetPrivateFieldWithPotentiallyTrimmedMetadata(this Type type, string fieldName)
    {
        FieldInfo? field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field is null)
        {
            throw new InvalidOperationException(
                $"Could not resolve metadata for field '{fieldName}' in type '{type}'. " +
                "If running Native AOT ensure that the 'IlcTrimMetadata' property has been disabled.");
        }

        return field;
    }

    // Resolves the parameters of the deserialization constructor for a type, if they exist.
#if NETCOREAPP
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "The deserialization constructor should have already been referenced by the source generator and therefore will not have been trimmed.")]
#endif
    private static Func<JsonPropertyInfo, ParameterInfo?> ResolveJsonConstructorParameterMapper(JsonTypeInfo typeInfo)
    {
        Debug.Assert(typeInfo.Kind is JsonTypeInfoKind.Object);

        if (typeInfo.Properties.Count > 0 &&
            typeInfo.CreateObject is null && // Ensure that a default constructor isn't being used
            typeInfo.Type.TryGetDeserializationConstructor(useDefaultCtorInAnnotatedStructs: true, out ConstructorInfo? ctor))
        {
            ParameterInfo[]? parameters = ctor?.GetParameters();
            if (parameters?.Length > 0)
            {
                Dictionary<ParameterLookupKey, ParameterInfo> dict = new(parameters.Length);
                foreach (ParameterInfo parameter in parameters)
                {
                    if (parameter.Name is not null)
                    {
                        // We don't care about null parameter names or conflicts since they
                        // would have already been rejected by JsonTypeInfo configuration.
                        dict[new(parameter.Name, parameter.ParameterType)] = parameter;
                    }
                }

                return prop => dict.TryGetValue(new(prop.Name, prop.PropertyType), out ParameterInfo? parameter) ? parameter : null;
            }
        }

        return static _ => null;
    }

    // Parameter to property matching semantics as declared in
    // https://github.com/dotnet/runtime/blob/12d96ccfaed98e23c345188ee08f8cfe211c03e7/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Metadata/JsonTypeInfo.cs#L1007-L1030
    private readonly struct ParameterLookupKey : IEquatable<ParameterLookupKey>
    {
        public ParameterLookupKey(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
        public bool Equals(ParameterLookupKey other) => Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        public override bool Equals(object? obj) => obj is ParameterLookupKey key && Equals(key);
    }

    // Resolves the deserialization constructor for a type using logic copied from
    // https://github.com/dotnet/runtime/blob/e12e2fa6cbdd1f4b0c8ad1b1e2d960a480c21703/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L227-L286
    private static bool TryGetDeserializationConstructor(
#if NETCOREAPP
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#endif
        this Type type,
        bool useDefaultCtorInAnnotatedStructs,
        out ConstructorInfo? deserializationCtor)
    {
        ConstructorInfo? ctorWithAttribute = null;
        ConstructorInfo? publicParameterlessCtor = null;
        ConstructorInfo? lonePublicCtor = null;

        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 1)
        {
            lonePublicCtor = constructors[0];
        }

        foreach (ConstructorInfo constructor in constructors)
        {
            if (HasJsonConstructorAttribute(constructor))
            {
                if (ctorWithAttribute != null)
                {
                    deserializationCtor = null;
                    return false;
                }

                ctorWithAttribute = constructor;
            }
            else if (constructor.GetParameters().Length == 0)
            {
                publicParameterlessCtor = constructor;
            }
        }

        // Search for non-public ctors with [JsonConstructor].
        foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (HasJsonConstructorAttribute(constructor))
            {
                if (ctorWithAttribute != null)
                {
                    deserializationCtor = null;
                    return false;
                }

                ctorWithAttribute = constructor;
            }
        }

        // Structs will use default constructor if attribute isn't used.
        if (useDefaultCtorInAnnotatedStructs && type.IsValueType && ctorWithAttribute == null)
        {
            deserializationCtor = null;
            return true;
        }

        deserializationCtor = ctorWithAttribute ?? publicParameterlessCtor ?? lonePublicCtor;
        return true;

        static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo) =>
            constructorInfo.GetCustomAttribute<JsonConstructorAttribute>() != null;
    }

    private static bool IsBuiltInConverter(JsonConverter converter) =>
        converter.GetType().Assembly == typeof(JsonConverter).Assembly;

    // Resolves the nullable reference type annotations for a property or field,
    // additionally addressing a few known bugs of the NullabilityInfo pre .NET 9.
    private static NullabilityInfo GetMemberNullability(this NullabilityInfoContext context, MemberInfo memberInfo)
    {
        Debug.Assert(memberInfo is PropertyInfo or FieldInfo);
        return memberInfo is PropertyInfo prop
            ? context.Create(prop)
            : context.Create((FieldInfo)memberInfo);
    }

    private static NullabilityState GetParameterNullability(this NullabilityInfoContext context, ParameterInfo parameterInfo)
    {
        // Workaround for https://github.com/dotnet/runtime/issues/92487
        if (parameterInfo.GetGenericParameterDefinition() is { ParameterType: { IsGenericParameter: true } typeParam })
        {
            // Step 1. Look for nullable annotations on the type parameter.
            if (GetNullableFlags(typeParam) is byte[] flags)
            {
                return TranslateByte(flags[0]);
            }

            // Step 2. Look for nullable annotations on the generic method declaration.
            if (typeParam.DeclaringMethod != null && GetNullableContextFlag(typeParam.DeclaringMethod) is byte flag)
            {
                return TranslateByte(flag);
            }

            // Step 3. Look for nullable annotations on the generic method declaration.
            if (GetNullableContextFlag(typeParam.DeclaringType!) is byte flag2)
            {
                return TranslateByte(flag2);
            }

            // Default to nullable.
            return NullabilityState.Nullable;

#if NETCOREAPP
            [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
                Justification = "We're resolving private fields of the built-in enum converter which cannot have been trimmed away.")]
#endif
            static byte[]? GetNullableFlags(MemberInfo member)
            {
                Attribute? attr = member.GetCustomAttributes().FirstOrDefault(attr =>
                {
                    Type attrType = attr.GetType();
                    return attrType.Namespace == "System.Runtime.CompilerServices" && attrType.Name == "NullableAttribute";
                });

                return (byte[])attr?.GetType().GetField("NullableFlags")?.GetValue(attr)!;
            }

#if NETCOREAPP
            [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
                Justification = "We're resolving private fields of the built-in enum converter which cannot have been trimmed away.")]
#endif
            static byte? GetNullableContextFlag(MemberInfo member)
            {
                Attribute? attr = member.GetCustomAttributes().FirstOrDefault(attr =>
                {
                    Type attrType = attr.GetType();
                    return attrType.Namespace == "System.Runtime.CompilerServices" && attrType.Name == "NullableContextAttribute";
                });

                return (byte?)attr?.GetType().GetField("Flag")?.GetValue(attr)!;
            }

            static NullabilityState TranslateByte(byte b) =>
                b switch
                {
                    1 => NullabilityState.NotNull,
                    2 => NullabilityState.Nullable,
                    _ => NullabilityState.Unknown
                };
        }

        return context.Create(parameterInfo).WriteState;
    }

    private static ParameterInfo GetGenericParameterDefinition(this ParameterInfo parameter)
    {
        if (parameter.Member is { DeclaringType.IsConstructedGenericType: true }
                             or MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false })
        {
            var genericMethod = (MethodBase)parameter.Member.GetGenericMemberDefinition()!;
            return genericMethod.GetParameters()[parameter.Position];
        }

        return parameter;
    }

#if NETCOREAPP
    [UnconditionalSuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "Looking up the generic member definition of the provided member.")]
#endif
    private static MemberInfo GetGenericMemberDefinition(this MemberInfo member)
    {
        if (member is Type type)
        {
            return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
        }

        if (member.DeclaringType!.IsConstructedGenericType)
        {
            const BindingFlags AllMemberFlags =
                BindingFlags.Static | BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic;

            return member.DeclaringType.GetGenericTypeDefinition()
                .GetMember(member.Name, AllMemberFlags)
                .First(m => m.MetadataToken == member.MetadataToken);
        }

        if (member is MethodInfo { IsGenericMethod: true, IsGenericMethodDefinition: false } method)
        {
            return method.GetGenericMethodDefinition();
        }

        return member;
    }

    // Taken from https://github.com/dotnet/runtime/blob/903bc019427ca07080530751151ea636168ad334/src/libraries/System.Text.Json/Common/ReflectionExtensions.cs#L288-L317
    private static object? GetNormalizedDefaultValue(this ParameterInfo parameterInfo)
    {
        Type parameterType = parameterInfo.ParameterType;
        object? defaultValue = parameterInfo.DefaultValue;

        if (defaultValue is null)
        {
            return null;
        }

        // DBNull.Value is sometimes used as the default value (returned by reflection) of nullable params in place of null.
        if (defaultValue == DBNull.Value && parameterType != typeof(DBNull))
        {
            return null;
        }

        // Default values of enums or nullable enums are represented using the underlying type and need to be cast explicitly
        // cf. https://github.com/dotnet/runtime/issues/68647
        if (parameterType.IsEnum)
        {
            return Enum.ToObject(parameterType, defaultValue);
        }

        if (Nullable.GetUnderlyingType(parameterType) is Type underlyingType && underlyingType.IsEnum)
        {
            return Enum.ToObject(underlyingType, defaultValue);
        }

        return defaultValue;
    }
}
