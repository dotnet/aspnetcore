// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Web.Internal;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.JSInterop.WebAssembly;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed partial class DefaultWebAssemblyJSRuntime : WebAssemblyJSRuntime, IInternalWebJSInProcessRuntime
{
    public static readonly DefaultWebAssemblyJSRuntime Instance = new();

    private readonly RootTypeCache _rootComponentCache = new();

    public ElementReferenceContext ElementReferenceContext { get; }

    public event Action<RootComponentOperationBatch, string>? OnUpdateRootComponents;

    [DynamicDependency(nameof(InvokeDotNet))]
    [DynamicDependency(nameof(EndInvokeJS))]
    [DynamicDependency(nameof(BeginInvokeDotNet))]
    [DynamicDependency(nameof(ReceiveByteArrayFromJS))]
    [DynamicDependency(nameof(UpdateRootComponents))]
    [DynamicDependency(nameof(DispatchMouseEvent))]
    [DynamicDependency(nameof(DispatchDragEvent))]
    [DynamicDependency(nameof(DispatchKeyboardEvent))]
    [DynamicDependency(nameof(DispatchChangeEventString))]
    [DynamicDependency(nameof(DispatchChangeEventBool))]
    [DynamicDependency(nameof(DispatchChangeEventStringArray))]
    [DynamicDependency(nameof(DispatchFocusEvent))]
    [DynamicDependency(nameof(DispatchClipboardEvent))]
    [DynamicDependency(nameof(DispatchPointerEvent))]
    [DynamicDependency(nameof(DispatchWheelEvent))]
    [DynamicDependency(nameof(DispatchTouchEvent))]
    [DynamicDependency(nameof(DispatchProgressEvent))]
    [DynamicDependency(nameof(DispatchErrorEvent))]
    [DynamicDependency(nameof(DispatchEmptyEvent))]
    [DynamicDependency(nameof(DispatchEventJson))]
    [DynamicDependency(nameof(DispatchLocationChanged))]
    [DynamicDependency(nameof(DispatchLocationChanging))]
    [DynamicDependency(JsonSerialized, typeof(KeyValuePair<,>))]
    private DefaultWebAssemblyJSRuntime()
    {
        ElementReferenceContext = new WebElementReferenceContext(this);
        JsonSerializerOptions.Converters.Add(new ElementReferenceJsonConverter(ElementReferenceContext));
    }

    public JsonSerializerOptions ReadJsonSerializerOptions() => JsonSerializerOptions;

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

            var componentType = Instance._rootComponentCache.GetRootType(operation.Marker!.Value.Assembly!, operation.Marker.Value.TypeName!)
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

    string IInternalWebJSInProcessRuntime.InvokeJS(in JSInvocationInfo invocationInfo)
        => InvokeJS(invocationInfo);
}
