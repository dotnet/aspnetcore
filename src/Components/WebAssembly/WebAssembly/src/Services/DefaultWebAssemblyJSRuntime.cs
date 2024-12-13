// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.JSInterop.WebAssembly;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed partial class DefaultWebAssemblyJSRuntime : WebAssemblyJSRuntime, IInternalWebJSInProcessRuntime
{
    public static readonly DefaultWebAssemblyJSRuntime Instance = new();

    private readonly RootComponentTypeCache _rootComponentCache = new();

    public ElementReferenceContext ElementReferenceContext { get; }

    public event Action<RootComponentOperationBatch>? OnUpdateRootComponents;

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
    [DynamicDependency(JsonSerialized, typeof(RootComponentOperationBatch))]
    [DynamicDependency(JsonSerialized, typeof(ComponentMarkerKey))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The correct members will be preserved by the above DynamicDependency")]
    internal static RootComponentOperationBatch DeserializeOperations(string operationsJson)
    {
        var deserialized = JsonSerializer.Deserialize(
            operationsJson,
            WebAssemblyJsonSerializerContext.Default.RootComponentOperationBatch)!;

        for (var i = 0; i < deserialized.Operations.Length; i++)
        {
            var operation = deserialized.Operations[i];
            if (operation.Type == RootComponentOperationType.Remove)
            {
                continue;
            }

            if (operation.Marker == null)
            {
                throw new InvalidOperationException($"The component operation of type '{operation.Type}' requires a '{nameof(operation.Marker)}' to be specified.");
            }

            var componentType = Instance._rootComponentCache.GetRootComponent(operation.Marker!.Value.Assembly!, operation.Marker.Value.TypeName!)
                ?? throw new InvalidOperationException($"Root component type '{operation.Marker.Value.TypeName}' could not be found in the assembly '{operation.Marker.Value.Assembly}'.");
            var parameters = DeserializeComponentParameters(operation.Marker.Value);
            operation.Descriptor = new(componentType, parameters);
        }

        return deserialized;
    }

    static WebRootComponentParameters DeserializeComponentParameters(ComponentMarker marker)
    {
        var definitions = WebAssemblyComponentParameterDeserializer.GetParameterDefinitions(marker.ParameterDefinitions!);
        var values = WebAssemblyComponentParameterDeserializer.GetParameterValues(marker.ParameterValues!);
        var componentDeserializer = WebAssemblyComponentParameterDeserializer.Instance;
        var parameters = componentDeserializer.DeserializeParameters(definitions, values);

        return new(parameters, definitions, values.AsReadOnly());
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

    string IInternalWebJSInProcessRuntime.InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        => InvokeJS(identifier, argsJson, resultType, targetInstanceId);
}
