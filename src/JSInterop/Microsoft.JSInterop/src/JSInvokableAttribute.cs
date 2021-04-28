// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    internal delegate object?[] DeserializeJSInvokableArgs(JSRuntime runtime, string callingMethod, string json, Type[] parameterTypes);

    /// <summary>
    /// Identifies a .NET method as allowing invocation from JavaScript code.
    /// Any method marked with this attribute may receive arbitrary parameter values
    /// from untrusted callers. All inputs should be validated carefully.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class JSInvokableAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier for the method. The identifier must be unique within the scope
        /// of an assembly.
        ///
        /// If not set, the identifier is taken from the name of the method. In this case the
        /// method name must be unique within the assembly.
        /// </summary>
        public string? Identifier { get; }


        internal DeserializeJSInvokableArgs Serializer { get; private set; }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/> without setting
        /// an identifier for the method.
        /// </summary>
        public JSInvokableAttribute()
        {
            Serializer = ParseArguments;
        }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/> using the specified
        /// identifier.
        /// </summary>
        /// <param name="identifier">An identifier for the method, which must be unique within the scope of the assembly.</param>
        public JSInvokableAttribute(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(identifier));
            }

            Identifier = identifier;
            Serializer = ParseArguments;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serializationContext"></param>
        public JSInvokableAttribute(Type serializationContext)
        {
            JsonSerializerContext? context = null;
            Serializer = (a, b, c, d) =>
            {
                context ??= (JsonSerializerContext)Activator.CreateInstance(serializationContext, new object[] { a.JsonSerializerOptions })!;
                return ParseArgumentsWithContext(context, a, b, c, d);
            };
        }

        internal static object?[] ParseArguments(JSRuntime jsRuntime, string methodIdentifier, string arguments, Type[] parameterTypes)
        {
            if (parameterTypes.Length == 0)
            {
                return Array.Empty<object>();
            }

            var utf8JsonBytes = Encoding.UTF8.GetBytes(arguments);
            var reader = new Utf8JsonReader(utf8JsonBytes);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Invalid JSON");
            }

            var suppliedArgs = new object?[parameterTypes.Length];

            var index = 0;
            while (index < parameterTypes.Length && reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var parameterType = parameterTypes[index];
                if (reader.TokenType == JsonTokenType.StartObject && IsIncorrectDotNetObjectRefUse(parameterType, reader))
                {
                    throw new InvalidOperationException($"In call to '{methodIdentifier}', parameter of type '{parameterType.Name}' at index {(index + 1)} must be declared as type 'DotNetObjectRef<{parameterType.Name}>' to receive the incoming value.");
                }

                suppliedArgs[index] = JsonSerializer.Deserialize(ref reader, parameterType, jsRuntime.JsonSerializerOptions);
                index++;
            }

            if (index < parameterTypes.Length)
            {
                // If we parsed fewer parameters, we can always make a definitive claim about how many parameters were received.
                throw new ArgumentException($"The call to '{methodIdentifier}' expects '{parameterTypes.Length}' parameters, but received '{index}'.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                // Either we received more parameters than we expected or the JSON is malformed.
                throw new JsonException($"Unexpected JSON token {reader.TokenType}. Ensure that the call to `{methodIdentifier}' is supplied with exactly '{parameterTypes.Length}' parameters.");
            }

            return suppliedArgs;

            // Note that the JsonReader instance is intentionally not passed by ref (or an in parameter) since we want a copy of the original reader.
            static bool IsIncorrectDotNetObjectRefUse(Type parameterType, Utf8JsonReader jsonReader)
            {
                // Check for incorrect use of DotNetObjectRef<T> at the top level. We know it's
                // an incorrect use if there's a object that looks like { '__dotNetObject': <some number> },
                // but we aren't assigning to DotNetObjectRef{T}.
                if (jsonReader.Read() &&
                    jsonReader.TokenType == JsonTokenType.PropertyName &&
                    jsonReader.ValueTextEquals(DotNetDispatcher.DotNetObjectRefKey.EncodedUtf8Bytes))
                {
                    // The JSON payload has the shape we expect from a DotNetObjectRef instance.
                    return !parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(DotNetObjectReference<>);
                }

                return false;
            }
        }

        internal static object?[] ParseArgumentsWithContext(JsonSerializerContext context, JSRuntime jsRuntime, string methodIdentifier, string arguments, Type[] parameterTypes)
        {


            if (parameterTypes.Length == 0)
            {
                return Array.Empty<object>();
            }

            var utf8JsonBytes = Encoding.UTF8.GetBytes(arguments);
            var reader = new Utf8JsonReader(utf8JsonBytes);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Invalid JSON");
            }

            var suppliedArgs = new object?[parameterTypes.Length];

            var index = 0;
            while (index < parameterTypes.Length && reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var parameterType = parameterTypes[index];
                if (reader.TokenType == JsonTokenType.StartObject && IsIncorrectDotNetObjectRefUse(parameterType, reader))
                {
                    throw new InvalidOperationException($"In call to '{methodIdentifier}', parameter of type '{parameterType.Name}' at index {(index + 1)} must be declared as type 'DotNetObjectRef<{parameterType.Name}>' to receive the incoming value.");
                }

                suppliedArgs[index] = JsonSerializer.Deserialize(ref reader, parameterType, context);
                index++;
            }

            if (index < parameterTypes.Length)
            {
                // If we parsed fewer parameters, we can always make a definitive claim about how many parameters were received.
                throw new ArgumentException($"The call to '{methodIdentifier}' expects '{parameterTypes.Length}' parameters, but received '{index}'.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            {
                // Either we received more parameters than we expected or the JSON is malformed.
                throw new JsonException($"Unexpected JSON token {reader.TokenType}. Ensure that the call to `{methodIdentifier}' is supplied with exactly '{parameterTypes.Length}' parameters.");
            }

            return suppliedArgs;

            // Note that the JsonReader instance is intentionally not passed by ref (or an in parameter) since we want a copy of the original reader.
            static bool IsIncorrectDotNetObjectRefUse(Type parameterType, Utf8JsonReader jsonReader)
            {
                // Check for incorrect use of DotNetObjectRef<T> at the top level. We know it's
                // an incorrect use if there's a object that looks like { '__dotNetObject': <some number> },
                // but we aren't assigning to DotNetObjectRef{T}.
                if (jsonReader.Read() &&
                    jsonReader.TokenType == JsonTokenType.PropertyName &&
                    jsonReader.ValueTextEquals(DotNetDispatcher.DotNetObjectRefKey.EncodedUtf8Bytes))
                {
                    // The JSON payload has the shape we expect from a DotNetObjectRef instance.
                    return !parameterType.IsGenericType || parameterType.GetGenericTypeDefinition() != typeof(DotNetObjectReference<>);
                }

                return false;
            }
        }
    }
}
