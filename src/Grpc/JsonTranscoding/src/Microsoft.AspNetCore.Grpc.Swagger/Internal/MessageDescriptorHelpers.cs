// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf.Reflection;

namespace Microsoft.AspNetCore.Grpc.Swagger.Internal;

internal static class MessageDescriptorHelpers
{
    public static Type ResolveFieldType(FieldDescriptor field)
    {
        if (field.IsMap)
        {
            var mapFields = field.MessageType.Fields.InFieldNumberOrder();
            var valueType = GetTypeFromFieldType(mapFields[1]);
            return typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
        }
        else if (field.IsRepeated)
        {
            return typeof(IList<>).MakeGenericType(GetTypeFromFieldType(field));
        }
        else
        {
            return GetTypeFromFieldType(field);
        }
    }

    private static Type GetTypeFromFieldType(FieldDescriptor field)
    {
        switch (field.FieldType)
        {
            case FieldType.Double:
                return typeof(double);
            case FieldType.Float:
                return typeof(float);
            case FieldType.Int64:
                return typeof(long);
            case FieldType.UInt64:
                return typeof(ulong);
            case FieldType.Int32:
                return typeof(int);
            case FieldType.Fixed64:
                return typeof(long);
            case FieldType.Fixed32:
                return typeof(int);
            case FieldType.Bool:
                return typeof(bool);
            case FieldType.String:
                return typeof(string);
            case FieldType.Bytes:
                return typeof(string);
            case FieldType.UInt32:
                return typeof(uint);
            case FieldType.SFixed32:
                return typeof(int);
            case FieldType.SFixed64:
                return typeof(long);
            case FieldType.SInt32:
                return typeof(int);
            case FieldType.SInt64:
                return typeof(long);
            case FieldType.Enum:
                return field.EnumType.ClrType;
            case FieldType.Message:
                return field.MessageType.ClrType;
            default:
                throw new InvalidOperationException("Unexpected field type: " + field.FieldType);
        }
    }
}
