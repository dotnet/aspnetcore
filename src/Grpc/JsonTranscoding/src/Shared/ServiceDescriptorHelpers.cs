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

    internal static bool IsWrapperType(DescriptorBase m) =>
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

    public static bool TryResolveDescriptors(MessageDescriptor messageDescriptor, IList<string> path, bool allowJsonName, [NotNullWhen(true)]out List<FieldDescriptor>? fieldDescriptors)
    {
        fieldDescriptors = null;
        MessageDescriptor? currentDescriptor = messageDescriptor;

        foreach (var fieldName in path)
        {
            FieldDescriptor? field = null;
            if (currentDescriptor != null)
            {
                field = allowJsonName
                    ? GetFieldByName(currentDescriptor, fieldName)
                    : currentDescriptor.FindFieldByName(fieldName);
            }

            if (field == null)
            {
                fieldDescriptors = null;
                return false;
            }

            fieldDescriptors ??= new List<FieldDescriptor>();
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

    private static FieldDescriptor? GetFieldByName(MessageDescriptor messageDescriptor, string fieldName)
    {
        // Search fields by field name and JSON name. Both names can be referenced.
        // If there are conflicts, then the last field with a name wins.
        // This logic matches how properties are used in JSON serialization's MessageTypeInfoResolver.
        var fields = messageDescriptor.Fields.InFieldNumberOrder();

        FieldDescriptor? fieldDescriptor = null;
        for (var i = fields.Count - 1; i >= 0; i--)
        {
            // We're checking JSON name first, in reverse order through fields.
            // That means the method can exit early on match because the match has the highest precedence.
            var field = fields[i];
            if (field.JsonName == fieldName)
            {
                fieldDescriptor = field;
                break;
            }
            else if (field.Name == fieldName)
            {
                fieldDescriptor = field;
                break;
            }
        }

        return fieldDescriptor;
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
                SetValue(currentValue, field, values);
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

    public static void SetValue(IMessage message, FieldDescriptor field, object? values)
    {
        if (field.IsRepeated)
        {
            var list = (IList)field.Accessor.GetValue(message);
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
                    field.Accessor.SetValue(message, ConvertValue(stringValues[0], field));
                }
                else
                {
                    throw new InvalidOperationException("Can't set multiple values onto a non-repeating field.");
                }
            }
            else if (values is IMessage messageValue)
            {
                if (IsWrapperType(messageValue.Descriptor))
                {
                    const int WrapperValueFieldNumber = Int32Value.ValueFieldNumber;

                    var wrappedValue = messageValue.Descriptor.Fields[WrapperValueFieldNumber].Accessor.GetValue(messageValue);
                    field.Accessor.SetValue(message, wrappedValue);
                }
                else
                {
                    field.Accessor.SetValue(message, messageValue);
                }
            }
            else
            {
                field.Accessor.SetValue(message, ConvertValue(values, field));
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

    public static Dictionary<string, RouteParameter> ResolveRouteParameterDescriptors(
        List<HttpRouteVariable> variables,
        MessageDescriptor messageDescriptor)
    {
        var routeParameterDescriptors = new Dictionary<string, RouteParameter>(StringComparer.Ordinal);
        foreach (var variable in variables)
        {
            var path = variable.FieldPath;
            if (!TryResolveDescriptors(messageDescriptor, path, allowJsonName: false, out var fieldDescriptors))
            {
                throw new InvalidOperationException($"Couldn't find matching field for route parameter '{string.Join(".", path)}' on {messageDescriptor.Name}.");
            }

            var completeFieldPath = string.Join(".", fieldDescriptors.Select(d => d.Name));
            var completeJsonPath = string.Join(".", fieldDescriptors.Select(d => d.JsonName));
            routeParameterDescriptors.Add(completeFieldPath, new RouteParameter(fieldDescriptors, variable, completeJsonPath));
        }

        return routeParameterDescriptors;
    }

    public static BodyDescriptorInfo? ResolveBodyDescriptor(string body, Type serviceType, MethodDescriptor methodDescriptor)
    {
        if (!string.IsNullOrEmpty(body))
        {
            if (!string.Equals(body, "*", StringComparison.Ordinal))
            {
                if (body.Contains('.', StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"The body field '{body}' references a nested field. The body field name must be on the top-level request message.");
                }
                var responseBodyDescriptor = methodDescriptor.InputType.FindFieldByName(body);
                if (responseBodyDescriptor == null)
                {
                    throw new InvalidOperationException($"Couldn't find matching field for body '{body}' on {methodDescriptor.InputType.Name}.");
                }

                var propertyName = FormatUnderscoreName(responseBodyDescriptor.Name, pascalCase: true, preservePeriod: false);
                var propertyInfo = responseBodyDescriptor.ContainingType.ClrType.GetProperty(propertyName);

                if (responseBodyDescriptor.IsRepeated)
                {
                    // A repeating field isn't a message type. The JSON parser will parse using the containing
                    // type to get the repeating collection.
                    return new BodyDescriptorInfo(responseBodyDescriptor.ContainingType, responseBodyDescriptor, IsDescriptorRepeated: true, propertyInfo);
                }
                else
                {
                    return new BodyDescriptorInfo(responseBodyDescriptor.MessageType, responseBodyDescriptor, IsDescriptorRepeated: false, propertyInfo);
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

                return new BodyDescriptorInfo(methodDescriptor.InputType, FieldDescriptor: null, IsDescriptorRepeated: false, ParameterInfo: requestParameter);
            }
        }

        return null;
    }

    public static Dictionary<string, FieldDescriptor> ResolveQueryParameterDescriptors(
        Dictionary<string, RouteParameter> routeParameters,
        MethodDescriptor methodDescriptor,
        MessageDescriptor? bodyDescriptor,
        FieldDescriptor? bodyFieldDescriptor)
    {
        var existingParameters = new List<FieldDescriptor>();

        foreach (var routeParameter in routeParameters)
        {
            // Each route field descriptors collection contains all the descriptors in the path.
            // We only care about the final place the route value is set and so add only the last
            // descriptor to the existing parameters collection.
            existingParameters.Add(routeParameter.Value.DescriptorsPath.Last());
        }

        if (bodyDescriptor != null)
        {
            if (bodyFieldDescriptor != null)
            {
                // Body with field name.
                existingParameters.Add(bodyFieldDescriptor);
            }
            else
            {
                // Body with wildcard. All parameters are in the body so no query parameters.
                return new Dictionary<string, FieldDescriptor>();
            }
        }

        var queryParameters = new Dictionary<string, FieldDescriptor>();
        RecursiveVisitMessages(queryParameters, existingParameters, methodDescriptor.InputType, new List<FieldDescriptor>());
        return queryParameters;

        static void RecursiveVisitMessages(Dictionary<string, FieldDescriptor> queryParameters, List<FieldDescriptor> existingParameters, MessageDescriptor messageDescriptor, List<FieldDescriptor> path)
        {
            var messageFields = messageDescriptor.Fields.InFieldNumberOrder();

            foreach (var fieldDescriptor in messageFields)
            {
                // If a field is set via route parameter or body then don't add query parameter.
                if (existingParameters.Contains(fieldDescriptor))
                {
                    continue;
                }

                // Add current field descriptor. It should be included in the path.
                path.Add(fieldDescriptor);

                switch (fieldDescriptor.FieldType)
                {
                    case FieldType.Double:
                    case FieldType.Float:
                    case FieldType.Int64:
                    case FieldType.UInt64:
                    case FieldType.Int32:
                    case FieldType.Fixed64:
                    case FieldType.Fixed32:
                    case FieldType.Bool:
                    case FieldType.String:
                    case FieldType.Bytes:
                    case FieldType.UInt32:
                    case FieldType.SFixed32:
                    case FieldType.SFixed64:
                    case FieldType.SInt32:
                    case FieldType.SInt64:
                    case FieldType.Enum:
                        var joinedPath = string.Join(".", path.Select(d => d.JsonName));
                        queryParameters[joinedPath] = fieldDescriptor;
                        break;
                    case FieldType.Group:
                    case FieldType.Message:
                    default:
                        // Complex repeated fields aren't valid query parameters.
                        if (!fieldDescriptor.IsRepeated)
                        {
                            RecursiveVisitMessages(queryParameters, existingParameters, fieldDescriptor.MessageType, path);
                        }
                        break;
                }

                // Remove current field descriptor.
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    public sealed record BodyDescriptorInfo(
        MessageDescriptor Descriptor,
        FieldDescriptor? FieldDescriptor,
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

internal record RouteParameter(
    List<FieldDescriptor> DescriptorsPath,
    HttpRouteVariable RouteVariable,
    string JsonPath);
