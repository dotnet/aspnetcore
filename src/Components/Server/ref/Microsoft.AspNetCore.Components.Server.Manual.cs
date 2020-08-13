// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ComponentParameter
    {
        private object _dummy;
        public string Assembly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public static (System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> parameterDefinitions, System.Collections.Generic.IList<object> parameterValues) FromParameterView(Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
    internal partial class ComponentParametersTypeCache
    {
        public ComponentParametersTypeCache() { }
        public System.Type GetParameterType(string assembly, string type) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ServerComponent
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ServerComponent(int sequence, string assemblyName, string typeName, System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> parametersDefinitions, System.Collections.Generic.IList<object> parameterValues, System.Guid invocationId) { throw null; }
        public string AssemblyName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Guid InvocationId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> ParameterDefinitions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IList<object> ParameterValues { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Sequence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string TypeName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal static partial class ServerComponentSerializationSettings
    {
        public static readonly System.TimeSpan DataExpiration;
        public const string DataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.ComponentDescriptorSerializer,V1";
        public static readonly System.Text.Json.JsonSerializerOptions JsonSerializationOptions;
    }
    internal sealed partial class ElementReferenceJsonConverter : System.Text.Json.Serialization.JsonConverter<Microsoft.AspNetCore.Components.ElementReference>
    {
        public ElementReferenceJsonConverter() { }
        public override Microsoft.AspNetCore.Components.ElementReference Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) { throw null; }
        public override void Write(System.Text.Json.Utf8JsonWriter writer, Microsoft.AspNetCore.Components.ElementReference value, System.Text.Json.JsonSerializerOptions options) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ServerComponentMarker
    {
        private object _dummy;
        private int _dummyPrimitive;
        public string Descriptor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PrerenderId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int? Sequence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.ServerComponentMarker GetEndRecord() { throw null; }
        public static Microsoft.AspNetCore.Components.ServerComponentMarker NonPrerendered(int sequence, string descriptor) { throw null; }
        public static Microsoft.AspNetCore.Components.ServerComponentMarker Prerendered(int sequence, string descriptor) { throw null; }
    }
    internal partial class ServerComponentTypeCache
    {
        public ServerComponentTypeCache() { }
        public System.Type GetRootComponent(string assembly, string type) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Server
{
    internal partial class CircuitDisconnectMiddleware
    {
        public CircuitDisconnectMiddleware(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.CircuitDisconnectMiddleware> logger, Microsoft.AspNetCore.Components.Server.Circuits.CircuitRegistry registry, Microsoft.AspNetCore.Components.Server.Circuits.CircuitIdFactory circuitIdFactory, Microsoft.AspNetCore.Http.RequestDelegate next) { }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitIdFactory CircuitIdFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.CircuitDisconnectMiddleware> Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.RequestDelegate Next { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitRegistry Registry { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext context) { throw null; }
    }
    internal partial class ServerComponentDeserializer
    {
        public ServerComponentDeserializer(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.ServerComponentDeserializer> logger, Microsoft.AspNetCore.Components.ServerComponentTypeCache rootComponentTypeCache, Microsoft.AspNetCore.Components.Server.ComponentParameterDeserializer parametersDeserializer) { }
        public bool TryDeserializeComponentDescriptorCollection(string serializedComponentRecords, out System.Collections.Generic.List<Microsoft.AspNetCore.Components.Server.ComponentDescriptor> descriptors) { throw null; }
    }
    internal partial class ComponentDescriptor
    {
        public ComponentDescriptor() { }
        public System.Type ComponentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.ParameterView Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int Sequence { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Deconstruct(out System.Type componentType, out Microsoft.AspNetCore.Components.ParameterView parameters, out int sequence) { throw null; }
    }
    internal sealed partial class ComponentHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public ComponentHub(Microsoft.AspNetCore.Components.Server.ServerComponentDeserializer serializer, Microsoft.AspNetCore.Components.Server.Circuits.CircuitFactory circuitFactory, Microsoft.AspNetCore.Components.Server.Circuits.CircuitIdFactory circuitIdFactory, Microsoft.AspNetCore.Components.Server.Circuits.CircuitRegistry circuitRegistry, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.ComponentHub> logger) { }
        public static Microsoft.AspNetCore.Http.PathString DefaultPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<bool> ConnectCircuit(string circuitIdSecret) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask DispatchBrowserEvent(string eventDescriptor, string eventArgs) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask EndInvokeJSFromDotNet(long asyncHandle, bool succeeded, string arguments) { throw null; }
        public override System.Threading.Tasks.Task OnDisconnectedAsync(System.Exception exception) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask OnLocationChanged(string uri, bool intercepted) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask OnRenderCompleted(long renderId, string errorMessageOrNull) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<string> StartCircuit(string baseUri, string uri, string serializedComponentRecords) { throw null; }
    }
    internal partial class ComponentParameterDeserializer
    {
        public ComponentParameterDeserializer(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.ComponentParameterDeserializer> logger, Microsoft.AspNetCore.Components.ComponentParametersTypeCache parametersCache) { }
        public bool TryDeserializeParameters(System.Collections.Generic.IList<Microsoft.AspNetCore.Components.ComponentParameter> parametersDefinitions, System.Collections.Generic.IList<object> parameterValues, out Microsoft.AspNetCore.Components.ParameterView parameters) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Server.BlazorPack
{
    internal sealed partial class BlazorPackHubProtocol : Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol
    {
        internal const string ProtocolName = "blazorpack";
        public BlazorPackHubProtocol() { }
        public string Name { get { throw null; } }
        public Microsoft.AspNetCore.Connections.TransferFormat TransferFormat { get { throw null; } }
        public int Version { get { throw null; } }
        public System.ReadOnlyMemory<byte> GetMessageBytes(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { throw null; }
        public bool IsVersionSupported(int version) { throw null; }
        public bool TryParseMessage(ref System.Buffers.ReadOnlySequence<byte> input, Microsoft.AspNetCore.SignalR.IInvocationBinder binder, out Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { throw null; }
        public void WriteMessage(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Buffers.IBufferWriter<byte> output) { }
    }
}
namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    internal partial class CircuitFactory
    {
        public CircuitFactory(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Components.Server.Circuits.CircuitIdFactory circuitIdFactory, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Components.Server.CircuitOptions> options) { }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost CreateCircuitHost(System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.Server.ComponentDescriptor> components, Microsoft.AspNetCore.Components.Server.Circuits.CircuitClientProxy client, string baseUri, string uri, System.Security.Claims.ClaimsPrincipal user) { throw null; }
    }
    internal partial class RenderBatchWriter : System.IDisposable
    {
        public RenderBatchWriter(System.IO.Stream output, bool leaveOpen) { }
        public void Dispose() { }
        public void Write(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch renderBatch) { }
    }
    internal partial class CircuitRegistry
    {
        public CircuitRegistry(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Components.Server.CircuitOptions> options, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.CircuitRegistry> logger, Microsoft.AspNetCore.Components.Server.Circuits.CircuitIdFactory CircuitHostFactory) { }
        internal System.Collections.Concurrent.ConcurrentDictionary<Microsoft.AspNetCore.Components.Server.Circuits.CircuitId, Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost> ConnectedCircuits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.Extensions.Caching.Memory.MemoryCache DisconnectedCircuits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost> ConnectAsync(Microsoft.AspNetCore.Components.Server.Circuits.CircuitId circuitId, Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string connectionId, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected virtual (Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost circuitHost, bool previouslyConnected) ConnectCore(Microsoft.AspNetCore.Components.Server.Circuits.CircuitId circuitId, Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string connectionId) { throw null; }
        public virtual System.Threading.Tasks.Task DisconnectAsync(Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost circuitHost, string connectionId) { throw null; }
        protected virtual bool DisconnectCore(Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost circuitHost, string connectionId) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        protected virtual void OnEntryEvicted(object key, object value, Microsoft.Extensions.Caching.Memory.EvictionReason reason, object state) { }
        public void Register(Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost circuitHost) { }
        public void RegisterDisconnectedCircuit(Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost circuitHost) { }
        public System.Threading.Tasks.ValueTask TerminateAsync(Microsoft.AspNetCore.Components.Server.Circuits.CircuitId circuitId) { throw null; }
    }
    internal partial class CircuitHandle
    {
        public CircuitHandle() { }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitHost CircuitHost { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class RemoteRenderer : Microsoft.AspNetCore.Components.RenderTree.Renderer
    {
        internal readonly System.Collections.Concurrent.ConcurrentQueue<Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer.UnacknowledgedRenderBatch> _unacknowledgedRenderBatches;
        public RemoteRenderer(System.IServiceProvider serviceProvider, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.Components.Server.CircuitOptions options, Microsoft.AspNetCore.Components.Server.Circuits.CircuitClientProxy client, Microsoft.Extensions.Logging.ILogger logger) : base (default(System.IServiceProvider), default(Microsoft.Extensions.Logging.ILoggerFactory)) { }
        public override Microsoft.AspNetCore.Components.Dispatcher Dispatcher { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public event System.EventHandler<System.Exception> UnhandledException { add { } remove { } }
        public System.Threading.Tasks.Task AddComponentAsync(System.Type componentType, string domElementSelector) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override void HandleException(System.Exception exception) { }
        public System.Threading.Tasks.Task OnRenderCompletedAsync(long incomingBatchId, string errorMessageOrNull) { throw null; }
        public System.Threading.Tasks.Task ProcessBufferedRenderBatches() { throw null; }
        protected override void ProcessPendingRender() { }
        protected override System.Threading.Tasks.Task UpdateDisplayAsync(in Microsoft.AspNetCore.Components.RenderTree.RenderBatch batch) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal readonly partial struct UnacknowledgedRenderBatch
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public UnacknowledgedRenderBatch(long batchId, Microsoft.AspNetCore.Components.Server.Circuits.ArrayBuilder<byte> data, System.Threading.Tasks.TaskCompletionSource<object> completionSource, Microsoft.Extensions.Internal.ValueStopwatch valueStopwatch) { throw null; }
            public long BatchId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public System.Threading.Tasks.TaskCompletionSource<object> CompletionSource { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public Microsoft.AspNetCore.Components.Server.Circuits.ArrayBuilder<byte> Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
            public Microsoft.Extensions.Internal.ValueStopwatch ValueStopwatch { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        }
    }
    internal partial class ArrayBuilder<T> : System.IDisposable
    {
        public ArrayBuilder(int minCapacity = 32, System.Buffers.ArrayPool<T> arrayPool = null) { }
        public T[] Buffer { get { throw null; } }
        public int Count { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public int Append(in T item) { throw null; }
        internal int Append(T[] source, int startIndex, int length) { throw null; }
        public void Clear() { }
        public void Dispose() { }
        public void InsertExpensive(int index, T value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Overwrite(int index, in T value) { }
        public void RemoveLast() { }
    }
    internal partial class CircuitClientProxy : Microsoft.AspNetCore.SignalR.IClientProxy
    {
        public CircuitClientProxy() { }
        public CircuitClientProxy(Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string connectionId) { }
        public Microsoft.AspNetCore.SignalR.IClientProxy Client { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Connected { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Threading.Tasks.Task SendCoreAsync(string method, object[] args, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void SetDisconnected() { }
        public void Transfer(Microsoft.AspNetCore.SignalR.IClientProxy clientProxy, string connectionId) { }
    }
    internal partial class CircuitHost : System.IAsyncDisposable
    {
        public CircuitHost(Microsoft.AspNetCore.Components.Server.Circuits.CircuitId circuitId, Microsoft.Extensions.DependencyInjection.IServiceScope scope, Microsoft.AspNetCore.Components.Server.CircuitOptions options, Microsoft.AspNetCore.Components.Server.Circuits.CircuitClientProxy client, Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer renderer, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.Server.ComponentDescriptor> descriptors, Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime jsRuntime, Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler[] circuitHandlers, Microsoft.Extensions.Logging.ILogger logger) { }
        public Microsoft.AspNetCore.Components.Server.Circuits.Circuit Circuit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitId CircuitId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitClientProxy Client { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.Server.ComponentDescriptor> Descriptors { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandle Handle { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime JSRuntime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Components.Server.Circuits.RemoteRenderer Renderer { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IServiceProvider Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public event System.UnhandledExceptionEventHandler UnhandledException { add { } remove { } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task DispatchEvent(string eventDescriptorJson, string eventArgsJson) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task EndInvokeJSFromDotNet(long asyncCall, bool succeded, string arguments) { throw null; }
        public System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnConnectionDownAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnConnectionUpAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnLocationChangedAsync(string uri, bool intercepted) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task OnRenderCompletedAsync(long renderId, string errorMessageOrNull) { throw null; }
        public void SendPendingBatches() { }
        public void SetCircuitUser(System.Security.Claims.ClaimsPrincipal user) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct CircuitId : System.IEquatable<Microsoft.AspNetCore.Components.Server.Circuits.CircuitId>
    {
        private readonly object _dummy;
        public CircuitId(string secret, string id) { throw null; }
        public string Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Secret { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool Equals([System.Diagnostics.CodeAnalysis.AllowNullAttribute]Microsoft.AspNetCore.Components.Server.Circuits.CircuitId other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    internal partial class CircuitIdFactory
    {
        public CircuitIdFactory(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider provider) { }
        public Microsoft.AspNetCore.Components.Server.Circuits.CircuitId CreateCircuitId() { throw null; }
        public bool TryParseCircuitId(string text, out Microsoft.AspNetCore.Components.Server.Circuits.CircuitId circuitId) { throw null; }
    }
    internal partial class RemoteJSRuntime : Microsoft.JSInterop.JSRuntime
    {
        public RemoteJSRuntime(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Components.Server.CircuitOptions> options, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime> logger) { }
        protected override void BeginInvokeJS(long asyncHandle, string identifier, string argsJson) { }
        protected override void EndInvokeDotNet(Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, in Microsoft.JSInterop.Infrastructure.DotNetInvocationResult invocationResult) { }
        internal void Initialize(Microsoft.AspNetCore.Components.Server.Circuits.CircuitClientProxy clientProxy) { }
        public static partial class Log
        {
            internal static void BeginInvokeJS(Microsoft.Extensions.Logging.ILogger logger, long asyncHandle, string identifier) { }
            internal static void InvokeDotNetMethodException(Microsoft.Extensions.Logging.ILogger logger, in Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, System.Exception exception) { }
            internal static void InvokeDotNetMethodSuccess(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.RemoteJSRuntime> logger, in Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo) { }
        }
    }
}
namespace Microsoft.AspNetCore.Internal
{
    internal static partial class BinaryMessageFormatter
    {
        public static int LengthPrefixLength(long length) { throw null; }
        public static void WriteLengthPrefix(long length, System.Buffers.IBufferWriter<byte> output) { }
        public static int WriteLengthPrefix(long length, System.Span<byte> output) { throw null; }
    }
    internal static partial class BinaryMessageParser
    {
        public static bool TryParseMessage(ref System.Buffers.ReadOnlySequence<byte> buffer, out System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
    }
    internal sealed partial class MemoryBufferWriter : System.IO.Stream, System.Buffers.IBufferWriter<byte>
    {
        public MemoryBufferWriter(int minimumSegmentSize = 4096) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public void Advance(int count) { }
        public void CopyTo(System.Buffers.IBufferWriter<byte> destination) { }
        public void CopyTo(System.Span<byte> span) { }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public static Microsoft.AspNetCore.Internal.MemoryBufferWriter Get() { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public void Reset() { }
        public static void Return(Microsoft.AspNetCore.Internal.MemoryBufferWriter writer) { }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public byte[] ToArray() { throw null; }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override void Write(System.ReadOnlySpan<byte> span) { }
        public override void WriteByte(byte value) { }
    }
}
namespace Microsoft.Extensions.Internal
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct ValueStopwatch
    {
        private int _dummyPrimitive;
        public bool IsActive { get { throw null; } }
        public System.TimeSpan GetElapsedTime() { throw null; }
        public static Microsoft.Extensions.Internal.ValueStopwatch StartNew() { throw null; }
    }
}
