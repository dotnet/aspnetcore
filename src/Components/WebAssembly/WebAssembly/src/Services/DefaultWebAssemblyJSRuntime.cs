// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.JSInterop.WebAssembly;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed partial class DefaultWebAssemblyJSRuntime : WebAssemblyJSRuntime
{
    private readonly RootComponentTypeCache _rootComponentCache = new();
    internal static readonly DefaultWebAssemblyJSRuntime Instance = new();

    public ElementReferenceContext ElementReferenceContext { get; }

    public event Action<OperationDescriptor[]>? OnUpdateRootComponents;

    [DynamicDependency(nameof(InvokeDotNet))]
    [DynamicDependency(nameof(EndInvokeJS))]
    [DynamicDependency(nameof(BeginInvokeDotNet))]
    [DynamicDependency(nameof(ReceiveByteArrayFromJS))]
    [DynamicDependency(nameof(UpdateRootComponentsCore))]
    private DefaultWebAssemblyJSRuntime()
    {
        ElementReferenceContext = new WebElementReferenceContext(this);
        JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
    }

    public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

    [JSExport]
    [SupportedOSPlatform("browser")]
    public static string? InvokeDotNet(
        string? assemblyName,
        string methodIdentifier,
        [JSMarshalAs<JSType.Number>] long dotNetObjectId,
        string argsJson)
    {
        var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId: null);
        return DotNetDispatcher.Invoke(Instance, callInfo, argsJson);
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    public static void EndInvokeJS(string argsJson)
    {
        WebAssemblyCallQueue.Schedule(argsJson, static argsJson =>
        {
            // This is not expected to throw, as it takes care of converting any unhandled user code
            // exceptions into a failure on the Task that was returned when calling InvokeAsync.
            DotNetDispatcher.EndInvokeJS(Instance, argsJson);
        });
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    public static void BeginInvokeDotNet(string? callId, string assemblyNameOrDotNetObjectId, string methodIdentifier, string argsJson)
    {
        // Figure out whether 'assemblyNameOrDotNetObjectId' is the assembly name or the instance ID
        // We only need one for any given call. This helps to work around the limitation that we can
        // only pass a maximum of 4 args in a call from JS to Mono WebAssembly.
        string? assemblyName;
        long dotNetObjectId;
        if (char.IsDigit(assemblyNameOrDotNetObjectId[0]))
        {
            dotNetObjectId = long.Parse(assemblyNameOrDotNetObjectId, CultureInfo.InvariantCulture);
            assemblyName = null;
        }
        else
        {
            dotNetObjectId = default;
            assemblyName = assemblyNameOrDotNetObjectId;
        }

        var callInfo = new DotNetInvocationInfo(assemblyName, methodIdentifier, dotNetObjectId, callId);
        WebAssemblyCallQueue.Schedule((callInfo, argsJson), static state =>
        {
            // This is not expected to throw, as it takes care of converting any unhandled user code
            // exceptions into a failure on the JS Promise object.
            DotNetDispatcher.BeginInvokeDotNet(Instance, state.callInfo, state.argsJson);
        });
    }

    [SupportedOSPlatform("browser")]
    [JSExport]
    public static void UpdateRootComponentsCore(string operationsJson)
    {
        try
        {
            var operations = DeserializeOperations(operationsJson);
            Instance.OnUpdateRootComponents?.Invoke(operations);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deserializing root component operations: {ex}");
        }
    }

    [DynamicDependency(JsonSerialized, typeof(RootComponentOperation))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The correct members will be preserved by the above DynamicDependency")]
    internal static OperationDescriptor[] DeserializeOperations(string operationsJson)
    {
        var deserialized = JsonSerializer.Deserialize<RootComponentOperation[]>(
            operationsJson,
            WebAssemblyComponentSerializationSettings.JsonSerializationOptions)!;

        var operations = new OperationDescriptor[deserialized.Length];

        for (var i = 0; i < deserialized.Length; i++)
        {
            var operation = deserialized[i];
            if (operation.Type == RootComponentOperationType.Remove ||
                operation.Type == RootComponentOperationType.Update)
            {
                if (operation.ComponentId == null)
                {
                    throw new InvalidOperationException($"The component operation of type '{operation.Type}' requires a '{nameof(operation.ComponentId)}' to be specified.");
                }
            }

            if (operation.Type == RootComponentOperationType.Remove)
            {
                operations[i] = new(operation, null, ParameterView.Empty);
                continue;
            }

            if (operation.Marker == null)
            {
                throw new InvalidOperationException($"The component operation of type '{operation.Type}' requires a '{nameof(operation.Marker)}' to be specified.");
            }

            Type? componentType = null;
            if (operation.Type == RootComponentOperationType.Add ||
                operation.Type == RootComponentOperationType.Update)
            {
                if (operation.SelectorId == null)
                {
                    throw new InvalidOperationException($"The component operation of type '{operation.Type}' requires a '{nameof(operation.SelectorId)}' to be specified.");
                }

                componentType = Instance._rootComponentCache.GetRootComponent(operation.Marker!.Value.Assembly!, operation.Marker.Value.TypeName!)
                ?? throw new InvalidOperationException($"Root component type '{operation.Marker.Value.TypeName}' could not be found in the assembly '{operation.Marker.Value.Assembly}'.");
            }

            var parameters = DeserializeComponentParameters(operation.Marker.Value);
            operations[i] = new(operation, componentType, parameters);
        }

        return operations;
    }

    static ParameterView DeserializeComponentParameters(ComponentMarker marker)
    {
        var definitions = WebAssemblyComponentParameterDeserializer.GetParameterDefinitions(marker.ParameterDefinitions!);
        var values = WebAssemblyComponentParameterDeserializer.GetParameterValues(marker.ParameterValues!);
        var componentDeserializer = WebAssemblyComponentParameterDeserializer.Instance;
        var parameters = componentDeserializer.DeserializeParameters(definitions, values);

        return parameters;
    }

    [JSExport]
    [SupportedOSPlatform("browser")]
    private static void ReceiveByteArrayFromJS(int id, byte[] data)
    {
        DotNetDispatcher.ReceiveByteArray(Instance, id, data);
    }

    /// <inheritdoc />
    protected override Task<Stream> ReadJSDataAsStreamAsync(IJSStreamReference jsStreamReference, long totalLength, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream>(PullFromJSDataStream.CreateJSDataStream(this, jsStreamReference, totalLength, cancellationToken));

    /// <inheritdoc />
    protected override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        return TransmitDataStreamToJS.TransmitStreamAsync(this, "Blazor._internal.receiveWebAssemblyDotNetDataStream", streamId, dotNetStreamReference);
    }
}

internal readonly struct OperationDescriptor
{
    public OperationDescriptor(
        RootComponentOperation operation,
        Type? componentType,
        ParameterView parameters)
    {
        Operation = operation;
        ComponentType = componentType;
        Parameters = parameters;
    }

    public RootComponentOperation Operation { get; }

    public Type? ComponentType { get; }

    public ParameterView Parameters { get; }

    public void Deconstruct(out RootComponentOperation operation, out Type? componentType, out ParameterView parameters)
    {
        operation = Operation;
        componentType = ComponentType;
        parameters = Parameters;
    }
}
