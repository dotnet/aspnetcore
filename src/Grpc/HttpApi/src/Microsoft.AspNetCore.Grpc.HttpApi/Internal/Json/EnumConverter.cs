// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Google.Protobuf.Reflection;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

internal sealed class EnumConverter<TEnum> : SettingsConverterBase<TEnum> where TEnum : Enum
{
    public EnumConverter(JsonSettings settings) : base(settings)
    {
    }

    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var enumDescriptor = ResolveEnumDescriptor(typeToConvert);
                if (enumDescriptor == null)
                {
                    throw new InvalidOperationException($"Unable to resolve descriptor for {typeToConvert}.");
                }
                var valueDescriptor = enumDescriptor.FindValueByName(reader.GetString()!);
                
                return ConvertFromInteger(valueDescriptor.Number);
            case JsonTokenType.Number:
                return ConvertFromInteger(reader.GetInt32());
            case JsonTokenType.Null:
                return default;
            default:
                throw new InvalidOperationException($"Unexpected JSON token: {reader.TokenType}.");
        }
    }

    private static EnumDescriptor? ResolveEnumDescriptor(Type typeToConvert)
    {
        var containingType = typeToConvert?.DeclaringType?.DeclaringType;

        if (containingType != null)
        {
            var messageDescriptor = JsonConverterHelper.GetMessageDescriptor(containingType);
            if (messageDescriptor != null)
            {
                for (var i = 0; i < messageDescriptor.EnumTypes.Count; i++)
                {
                    if (messageDescriptor.EnumTypes[i].ClrType == typeToConvert)
                    {
                        return messageDescriptor.EnumTypes[i];
                    }
                }
            }
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (Settings.FormatEnumsAsIntegers)
        {
            writer.WriteNumberValue(ConvertToInteger(value));
        }
        else
        {
            var name = Legacy.OriginalEnumValueHelper.GetOriginalName(value);
            if (name != null)
            {
                writer.WriteStringValue(name);
            }
            else
            {
                writer.WriteNumberValue(ConvertToInteger(value));
            }
        }
    }

    private static TEnum ConvertFromInteger(int integer)
    {
        if (!TryConvertToEnum(integer, out var value))
        {
            throw new InvalidOperationException($"Integer can't be converted to enum {typeof(TEnum).FullName}.");
        }

        return value;
    }

    private static int ConvertToInteger(TEnum value)
    {
        if (!TryConvertToInteger(value, out var integer))
        {
            throw new InvalidOperationException($"Enum {typeof(TEnum).FullName} can't be converted to integer.");
        }

        return integer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertToInteger(TEnum value, out int integer)
    {
        if (Unsafe.SizeOf<int>() == Unsafe.SizeOf<TEnum>())
        {
            integer = Unsafe.As<TEnum, int>(ref value);
            return true;
        }
        integer = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryConvertToEnum(int integer, [NotNullWhen(true)] out TEnum? value)
    {
        if (Unsafe.SizeOf<int>() == Unsafe.SizeOf<TEnum>())
        {
            value = Unsafe.As<int, TEnum>(ref integer);
            return true;
        }
        value = default;
        return false;
    }
}
