// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Google.Protobuf.Reflection;
using Grpc.Shared;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;

internal sealed class EnumConverter<TEnum> : SettingsConverterBase<TEnum> where TEnum : Enum
{
    public EnumConverter(JsonContext context) : base(context)
    {
    }

    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var enumDescriptor = (EnumDescriptor?)Context.DescriptorRegistry.FindDescriptorByType(typeToConvert);
                if (enumDescriptor == null)
                {
                    throw new InvalidOperationException($"Unable to resolve descriptor for {typeToConvert}.");
                }

                var value = reader.GetString()!;
                var valueDescriptor = enumDescriptor.FindValueByName(value);
                if (valueDescriptor == null)
                {
                    throw new InvalidOperationException(@$"Error converting value ""{value}"" to enum type {typeToConvert}.");
                }

                return ConvertFromInteger(valueDescriptor.Number);
            case JsonTokenType.Number:
                return ConvertFromInteger(reader.GetInt32());
            case JsonTokenType.Null:
                return default;
            default:
                throw new InvalidOperationException($"Unexpected JSON token: {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (Context.Settings.WriteEnumsAsIntegers)
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
