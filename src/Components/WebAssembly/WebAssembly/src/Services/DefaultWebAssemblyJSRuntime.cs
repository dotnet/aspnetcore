// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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

    private readonly RootTypeCache _rootComponentCache = new();
    private readonly LazyAssemblyLoader _lazyAssemblyLoader;
    private readonly HashSet<int> _failedRootComponentIds = new();

    internal Task PendingRootComponentUpdate { get; set; } = Task.CompletedTask;

    public ElementReferenceContext ElementReferenceContext { get; }

    public event Action<RootComponentOperationBatch, string>? OnUpdateRootComponents;

    [DynamicDependency(nameof(InvokeDotNet))]
    [DynamicDependency(nameof(EndInvokeJS))]
    [DynamicDependency(nameof(BeginInvokeDotNet))]
    [DynamicDependency(nameof(ReceiveByteArrayFromJS))]
    [DynamicDependency(nameof(UpdateRootComponentsCore))]
    [DynamicDependency(JsonSerialized, typeof(KeyValuePair<,>))]
    private DefaultWebAssemblyJSRuntime()
    {
        _lazyAssemblyLoader = new LazyAssemblyLoader(this);
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
    public static void EndInvokeJS([StringSyntax(StringSyntaxAttribute.Json)] string argsJson)
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
    public static void BeginInvokeDotNet(string? callId, string assemblyNameOrDotNetObjectId, string methodIdentifier, [StringSyntax(StringSyntaxAttribute.Json)] string argsJson)
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
    public static void UpdateRootComponentsCore([StringSyntax(StringSyntaxAttribute.Json)] string operationsJson, string appState)
    {
        _ = Instance.UpdateRootComponentsAsync(operationsJson, appState);
    }

    internal Task UpdateRootComponentsAsync(string operationsJson, string appState)
    {
        var previousUpdate = PendingRootComponentUpdate;
        var completion = new TaskCompletionSource();
        PendingRootComponentUpdate = completion.Task;
        _ = ProcessRootComponentUpdateAsync(previousUpdate, completion, operationsJson, appState);

        return completion.Task;
    }

    private async Task ProcessRootComponentUpdateAsync(Task previousUpdate, TaskCompletionSource completion, string operationsJson, string appState)
    {
        try
        {
            await previousUpdate;
            var operations = await DeserializeOperationsAsync(operationsJson, _lazyAssemblyLoader);
            OnUpdateRootComponents?.Invoke(operations, appState);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error deserializing root component operations: {ex}");
        }
        finally
        {
            completion.SetResult();
        }
    }

    [DynamicDependency(JsonSerialized, typeof(RootComponentOperation))]
    [DynamicDependency(JsonSerialized, typeof(RootComponentOperationBatch))]
    [DynamicDependency(JsonSerialized, typeof(ComponentMarkerKey))]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Root component and parameter types are preserved by the application.")]
    internal static async Task<RootComponentOperationBatch> DeserializeOperationsAsync(string operationsJson, LazyAssemblyLoader loader)
    {
        var batch = JsonSerializer.Deserialize(
            operationsJson,
            WebAssemblyJsonSerializerContext.Default.RootComponentOperationBatch)!;
        // Later batches can already be queued for roots whose additions failed.
        if (Instance._failedRootComponentIds.Count > 0)
        {
            batch.Operations = batch.Operations.Where(operation =>
            {
                if (!Instance._failedRootComponentIds.Contains(operation.SsrComponentId))
                {
                    return true;
                }

                if (operation.Type == RootComponentOperationType.Remove)
                {
                    Instance._failedRootComponentIds.Remove(operation.SsrComponentId);
                }

                return false;
            }).ToArray();
        }

        try
        {
            return await PreloadAndResolveOperationsAsync(batch, loader);
        }
        catch (Exception ex) when (OperatingSystem.IsBrowser())
        {
            Console.Error.WriteLine($"Error deserializing root component operations: {ex}");
            foreach (var operation in batch.Operations)
            {
                if (operation.Type == RootComponentOperationType.Add)
                {
                    Instance._failedRootComponentIds.Add(operation.SsrComponentId);
                }
            }

            // Failure is not an acknowledgment of additions or updates. The renderer
            // subsequently acknowledges only the removals that are actually dispatched.
            Instance.InvokeVoid("Blazor._internal.failUpdateRootComponents", batch.BatchId);
            batch.Operations = batch.Operations.Where(operation => operation.Type == RootComponentOperationType.Remove).ToArray();
            return batch;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Root component and parameter types are preserved by the application.")]
    private static async Task<RootComponentOperationBatch> PreloadAndResolveOperationsAsync(RootComponentOperationBatch batch, LazyAssemblyLoader loader)
    {
        var assemblyNames = batch.Operations
            .Where(operation => operation.Type != RootComponentOperationType.Remove && operation.Marker is not null)
            .SelectMany(operation => LazyAssemblyLoader.GetRequiredAssemblyNames(operation.Marker!.Value));

        await loader.LoadMissingAssembliesAsync(assemblyNames);
        Instance._rootComponentCache.ClearCache();

        return ResolveOperationDescriptors(batch);
    }

    // Resolves component types for all non-Remove operations in a pre-parsed batch.
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Root components are in assemblies that don't get trimmed.")]
    internal static RootComponentOperationBatch ResolveOperationDescriptors(RootComponentOperationBatch batch)
    {
        for (var i = 0; i < batch.Operations.Length; i++)
        {
            var operation = batch.Operations[i];
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

        return batch;
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

    string IInternalWebJSInProcessRuntime.InvokeJS(in JSInvocationInfo invocationInfo)
        => InvokeJS(invocationInfo);
}
