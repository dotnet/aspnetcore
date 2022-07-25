#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Google.Api;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;
using Type = System.Type;

namespace Grpc.Shared;

internal static class ServiceDescriptorHelpers
{
    private static readonly HashSet<string> WellKnownTypeNames = new HashSet<string>
    {
        "google/protobuf/any.proto",
        "google/protobuf/api.proto",
        "google/protobuf/duration.proto",
        "google/protobuf/empty.proto",
        "google/protobuf/wrappers.proto",
        "google/protobuf/timestamp.proto",
        "google/protobuf/field_mask.proto",
        "google/protobuf/source_context.proto",
        "google/protobuf/struct.proto",
        "google/protobuf/type.proto",
    };

    internal static bool IsWellKnownType(MessageDescriptor messageDescriptor) => messageDescriptor.File.Package == "google.protobuf" &&
        WellKnownTypeNames.Contains(messageDescriptor.File.Name);

    internal static bool IsWrapperType(MessageDescriptor m) =>
        m.File.Package == "google.protobuf" && m.File.Name == "google/protobuf/wrappers.proto";

    public static ServiceDescriptor? GetServiceDescriptor(Type serviceReflectionType)
    {
        var property = serviceReflectionType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static);
        if (property != null)
        {
            return (ServiceDescriptor?)property.GetValue(null);
        }

        throw new InvalidOperationException($"Get not find Descriptor property on {serviceReflectionType.Name}.");
    }

    public static bool TryResolveDescriptors(MessageDescriptor messageDescriptor, IList<string> path, [NotNullWhen(true)]out List<FieldDescriptor>? fieldDescriptors)
    {
        fieldDescriptors = null;
        MessageDescriptor? currentDescriptor = messageDescriptor;

        foreach (var fieldName in path)
        {
            var field = currentDescriptor?.FindFieldByName(fieldName);
            if (field == null)
            {
                fieldDescriptors = null;
                return false;
            }

            if (fieldDescriptors == null)
            {
                fieldDescriptors = new List<FieldDescriptor>();
            }

            fieldDescriptors.Add(field);
            if (field.FieldType == FieldType.Message)
            {
                currentDescriptor = field.MessageType;
            }
            else
            {
                currentDescriptor = null;
            }
        }

        return fieldDescriptors != null;
    }

    private static object? ConvertValue(object? value, FieldDescriptor descriptor)
    {
        switch (descriptor.FieldType)
        {
            case FieldType.Double:
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            case FieldType.Float:
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);
            case FieldType.Int64:
            case FieldType.SInt64:
            case FieldType.SFixed64:
                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            case FieldType.UInt64:
            case FieldType.Fixed64:
                return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
            case FieldType.Int32:
            case FieldType.SInt32:
            case FieldType.SFixed32:
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            case FieldType.Bool:
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            case FieldType.String:
                return value;
            case FieldType.Bytes:
                {
                    if (value is string s)
                    {
                        return ByteString.FromBase64(s);
                    }
                    throw new InvalidOperationException("Base64 encoded string required to convert to bytes.");
                }
            case FieldType.UInt32:
            case FieldType.Fixed32:
                return Convert.ToUInt32(value, CultureInfo.InvariantCulture);
            case FieldType.Enum:
                {
                    if (value is string s)
                    {
                        var enumValueDescriptor = int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                            ? descriptor.EnumType.FindValueByNumber(i)
                            : descriptor.EnumType.FindValueByName(s);

                        if (enumValueDescriptor == null)
                        {
                            throw new InvalidOperationException($"Invalid value '{s}' for enum type {descriptor.EnumType.Name}.");
                        }

                        return enumValueDescriptor.Number;
                    }
                    throw new InvalidOperationException("String required to convert to enum.");
                }
            case FieldType.Message:
                if (IsWrapperType(descriptor.MessageType))
                {
                    if (value == null)
                    {
                        return null;
                    }

                    return ConvertValue(value, descriptor.MessageType.FindFieldByName("value"));
                }
                break;
        }

        throw new InvalidOperationException("Unsupported type: " + descriptor.FieldType);
    }

    public static void RecursiveSetValue(IMessage currentValue, List<FieldDescriptor> pathDescriptors, object? values)
    {
        for (var i = 0; i < pathDescriptors.Count; i++)
        {
            var isLast = i == pathDescriptors.Count - 1;
            var field = pathDescriptors[i];

            if (isLast)
            {
                if (field.IsRepeated)
                {
                    var list = (IList)field.Accessor.GetValue(currentValue);
                    if (values is StringValues stringValues)
                    {
                        foreach (var value in stringValues)
                        {
                            list.Add(ConvertValue(value, field));
                        }
                    }
                    else if (values is IList listValues)
                    {
                        foreach (var value in listValues)
                        {
                            list.Add(ConvertValue(value, field));
                        }
                    }
                    else
                    {
                        list.Add(ConvertValue(values, field));
                    }
                }
                else
                {
                    if (values is StringValues stringValues)
                    {
                        if (stringValues.Count == 1)
                        {
                            field.Accessor.SetValue(currentValue, ConvertValue(stringValues[0], field));
                        }
                        else
                        {
                            throw new InvalidOperationException("Can't set multiple values onto a non-repeating field.");
                        }
                    }
                    else if (values is IMessage message)
                    {
                        if (IsWrapperType(message.Descriptor))
                        {
                            const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;

                            var wrappedValue = message.Descriptor.Fields[WrapperValueFieldNumber].Accessor.GetValue(message);
                            field.Accessor.SetValue(currentValue, wrappedValue);
                        }
                        else
                        {
                            field.Accessor.SetValue(currentValue, message);
                        }
                    }
                    else
                    {
                        field.Accessor.SetValue(currentValue, ConvertValue(values, field));
                    }
                }
            }
            else
            {
                var fieldMessage = (IMessage)field.Accessor.GetValue(currentValue);

                if (fieldMessage == null)
                {
                    fieldMessage = (IMessage)Activator.CreateInstance(field.MessageType.ClrType)!;
                    field.Accessor.SetValue(currentValue, fieldMessage);
                }

                currentValue = fieldMessage;
            }
        }
    }

    public static bool TryGetHttpRule(MethodDescriptor methodDescriptor, [NotNullWhen(true)]out HttpRule? httpRule)
    {
        var options = methodDescriptor.GetOptions();
        httpRule = options?.GetExtension(AnnotationsExtensions.Http);

        return httpRule != null;
    }

    public static bool TryResolvePattern(HttpRule http, [NotNullWhen(true)]out string? pattern, [NotNullWhen(true)]out string? verb)
    {
        switch (http.PatternCase)
        {
            case HttpRule.PatternOneofCase.Get:
                pattern = http.Get;
                verb = "GET";
                return true;
            case HttpRule.PatternOneofCase.Put:
                pattern = http.Put;
                verb = "PUT";
                return true;
            case HttpRule.PatternOneofCase.Post:
                pattern = http.Post;
                verb = "POST";
                return true;
            case HttpRule.PatternOneofCase.Delete:
                pattern = http.Delete;
                verb = "DELETE";
                return true;
            case HttpRule.PatternOneofCase.Patch:
                pattern = http.Patch;
                verb = "PATCH";
                return true;
            case HttpRule.PatternOneofCase.Custom:
                pattern = http.Custom.Path;
                verb = http.Custom.Kind;
                return true;
            default:
                pattern = null;
                verb = null;
                return false;
        }
    }

    public static Dictionary<string, List<FieldDescriptor>> ResolveRouteParameterDescriptors(List<List<string>> parameters, MessageDescriptor messageDescriptor)
    {
        var routeParameterDescriptors = new Dictionary<string, List<FieldDescriptor>>(StringComparer.Ordinal);
        foreach (var routeParameter in parameters)
        {
            var completeFieldPath = string.Join(".", routeParameter);
            if (!TryResolveDescriptors(messageDescriptor, routeParameter, out var fieldDescriptors))
            {
                throw new InvalidOperationException($"Couldn't find matching field for route parameter '{completeFieldPath}' on {messageDescriptor.Name}.");
            }

            routeParameterDescriptors.Add(completeFieldPath, fieldDescriptors);
        }

        return routeParameterDescriptors;
    }

    public static BodyDescriptorInfo? ResolveBodyDescriptor(string body, Type serviceType, MethodDescriptor methodDescriptor)
    {
        if (!string.IsNullOrEmpty(body))
        {
            if (!string.Equals(body, "*", StringComparison.Ordinal))
            {
                var bodyFieldPath = body.Split('.');
                if (!TryResolveDescriptors(methodDescriptor.InputType, bodyFieldPath, out var bodyFieldDescriptors))
                {
                    throw new InvalidOperationException($"Couldn't find matching field for body '{body}' on {methodDescriptor.InputType.Name}.");
                }
                var leafDescriptor = bodyFieldDescriptors.Last();
                var propertyName = FormatUnderscoreName(leafDescriptor.Name, pascalCase: true, preservePeriod: false);
                var propertyInfo = leafDescriptor.ContainingType.ClrType.GetProperty(propertyName);

                if (leafDescriptor.IsRepeated)
                {
                    // A repeating field isn't a message type. The JSON parser will parse using the containing
                    // type to get the repeating collection.
                    return new BodyDescriptorInfo(leafDescriptor.ContainingType, bodyFieldDescriptors, IsDescriptorRepeated: true, propertyInfo);
                }
                else
                {
                    return new BodyDescriptorInfo(leafDescriptor.MessageType, bodyFieldDescriptors, IsDescriptorRepeated: false, propertyInfo);
                }
            }
            else
            {
                ParameterInfo? requestParameter = null;
                var methodInfo = serviceType.GetMethod(methodDescriptor.Name);
                if (methodInfo != null)
                {
                    requestParameter = methodInfo.GetParameters().SingleOrDefault(p => p.Name == "request");
                }

                return new BodyDescriptorInfo(methodDescriptor.InputType, FieldDescriptors: null, IsDescriptorRepeated: false, ParameterInfo: requestParameter);
            }
        }

        return null;
    }

    public sealed record BodyDescriptorInfo(
        MessageDescriptor Descriptor,
        List<FieldDescriptor>? FieldDescriptors,
        bool IsDescriptorRepeated,
        PropertyInfo? PropertyInfo = null,
        ParameterInfo? ParameterInfo = null);

    public static string FormatUnderscoreName(string input, bool pascalCase, bool preservePeriod)
    {
        var capitalizeNext = pascalCase;
        var result = string.Empty;

        for (var i = 0; i < input.Length; i++)
        {
            if (char.IsLower(input[i]))
            {
                if (capitalizeNext)
                {
                    result += char.ToUpper(input[i], CultureInfo.InvariantCulture);
                }
                else
                {
                    result += input[i];
                }
                capitalizeNext = false;
            }
            else if (char.IsUpper(input[i]))
            {
                if (i == 0 && !capitalizeNext)
                {
                    // Force first letter to lower-case unless explicitly told to
                    // capitalize it.
                    result += char.ToLower(input[i], CultureInfo.InvariantCulture);
                }
                else
                {
                    // Capital letters after the first are left as-is.
                    result += input[i];
                }
                capitalizeNext = false;
            }
            else if (char.IsDigit(input[i]))
            {
                result += input[i];
                capitalizeNext = true;
            }
            else
            {
                capitalizeNext = true;
                if (input[i] == '.' && preservePeriod)
                {
                    result += '.';
                }
            }
        }
        // Add a trailing "_" if the name should be altered.
        if (input.Length > 0 && input[input.Length - 1] == '#')
        {
            result += '_';
        }
        return result;
    }
}
