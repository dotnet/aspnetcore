// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Virtualization;

#pragma warning disable ASPNETCORE9004 // Built-in Native AOT metadata consumes the experimental descriptor model.

namespace Microsoft.JSInterop.Infrastructure;

internal static class BuiltInJSInvokableMethodDescriptors
{
    private const string AssemblyName = "Microsoft.AspNetCore.Components.Web";

    internal static JSInvokableMethodDescriptor[] GetDescriptors()
        =>
        [
            CreateInstance<WebRenderer.WebRendererInteropMethods>(
                nameof(WebRenderer.WebRendererInteropMethods.DispatchEventAsync),
                parameterCount: 2,
                static async (target, arguments, options) =>
                {
                    await target.DispatchEventAsync(
                        Read<JsonElement>(arguments, 0, options),
                        Read<JsonElement>(arguments, 1, options)).ConfigureAwait(false);
                    return null;
                }),
            CreateInstance<WebRenderer.WebRendererInteropMethods>(
                nameof(WebRenderer.WebRendererInteropMethods.AddRootComponent),
                parameterCount: 2,
                static (target, arguments, options) =>
                    new ValueTask<string?>(Write(
                        target.AddRootComponent(
                            Read<string>(arguments, 0, options),
                            Read<string>(arguments, 1, options)),
                        options))),
            CreateInstance<WebRenderer.WebRendererInteropMethods>(
                nameof(WebRenderer.WebRendererInteropMethods.SetRootComponentParameters),
                parameterCount: 3,
                static (target, arguments, options) =>
                {
                    target.SetRootComponentParameters(
                        Read<int>(arguments, 0, options),
                        Read<int>(arguments, 1, options),
                        Read<JsonElement>(arguments, 2, options));
                    return default;
                }),
            CreateInstance<WebRenderer.WebRendererInteropMethods>(
                nameof(WebRenderer.WebRendererInteropMethods.RemoveRootComponent),
                parameterCount: 1,
                static (target, arguments, options) =>
                {
                    target.RemoveRootComponent(Read<int>(arguments, 0, options));
                    return default;
                }),
            CreateInstance<VirtualizeJsInterop>(
                nameof(VirtualizeJsInterop.OnSpacerBeforeVisible),
                parameterCount: 3,
                static (target, arguments, options) =>
                {
                    target.OnSpacerBeforeVisible(
                        Read<float>(arguments, 0, options),
                        Read<float>(arguments, 1, options),
                        Read<float>(arguments, 2, options));
                    return default;
                }),
            CreateInstance<VirtualizeJsInterop>(
                nameof(VirtualizeJsInterop.OnSpacerAfterVisible),
                parameterCount: 3,
                static (target, arguments, options) =>
                {
                    target.OnSpacerAfterVisible(
                        Read<float>(arguments, 0, options),
                        Read<float>(arguments, 1, options),
                        Read<float>(arguments, 2, options));
                    return default;
                }),
            CreateInstance<InputFileJsCallbacksRelay>(
                nameof(InputFileJsCallbacksRelay.NotifyChange),
                parameterCount: 1,
                static async (target, arguments, options) =>
                {
                    await target.NotifyChange(Read<BrowserFile[]>(arguments, 0, options)).ConfigureAwait(false);
                    return null;
                }),
        ];

    private static JSInvokableMethodDescriptor CreateInstance<TTarget>(
        string identifier,
        int parameterCount,
        Func<TTarget, JsonElement, JsonSerializerOptions, ValueTask<string?>> invoke)
        where TTarget : class
        => new()
        {
            AssemblyName = AssemblyName,
            TargetType = typeof(TTarget),
            Identifier = identifier,
            IsStatic = false,
            MethodKey = $"{typeof(TTarget).FullName}.{identifier}",
            Kind = JSInvokableMethodKind.Method,
            Invoke = (target, argumentsJson, options) =>
            {
                using var document = JsonDocument.Parse(argumentsJson);
                ValidateArguments(document.RootElement, identifier, parameterCount);
                return invoke((TTarget)target!, document.RootElement, options);
            },
        };

    private static void ValidateArguments(JsonElement arguments, string identifier, int expectedCount)
    {
        if (arguments.ValueKind is not JsonValueKind.Array)
        {
            throw new JsonException("Invalid JSON");
        }

        var actualCount = arguments.GetArrayLength();
        if (actualCount < expectedCount)
        {
            throw new ArgumentException(
                $"The call to '{identifier}' expects '{expectedCount}' parameters, but received '{actualCount}'.");
        }

        if (actualCount > expectedCount)
        {
            throw new JsonException(
                $"Ensure that the call to '{identifier}' is supplied with exactly '{expectedCount}' parameters.");
        }
    }

    private static T Read<T>(JsonElement arguments, int index, JsonSerializerOptions options)
        => arguments[index].Deserialize((JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)))!;

    private static string Write<T>(T value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(value, (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T)));
}

#pragma warning restore ASPNETCORE9004
