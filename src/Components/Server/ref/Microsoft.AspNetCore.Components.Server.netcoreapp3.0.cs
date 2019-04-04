// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace MessagePack
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ExtensionHeader
    {
        private int _dummyPrimitive;
        public ExtensionHeader(sbyte typeCode, int length) { throw null; }
        public ExtensionHeader(sbyte typeCode, uint length) { throw null; }
        public uint Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public sbyte TypeCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ExtensionResult
    {
        private object _dummy;
        private int _dummyPrimitive;
        public ExtensionResult(sbyte typeCode, System.Buffers.ReadOnlySequence<byte> data) { throw null; }
        public ExtensionResult(sbyte typeCode, System.Memory<byte> data) { throw null; }
        public System.Buffers.ReadOnlySequence<byte> Data { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public MessagePack.ExtensionHeader Header { get { throw null; } }
        public sbyte TypeCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public static partial class MessagePackCode
    {
        public const byte Array16 = (byte)220;
        public const byte Array32 = (byte)221;
        public const byte Bin16 = (byte)197;
        public const byte Bin32 = (byte)198;
        public const byte Bin8 = (byte)196;
        public const byte Ext16 = (byte)200;
        public const byte Ext32 = (byte)201;
        public const byte Ext8 = (byte)199;
        public const byte False = (byte)194;
        public const byte FixExt1 = (byte)212;
        public const byte FixExt16 = (byte)216;
        public const byte FixExt2 = (byte)213;
        public const byte FixExt4 = (byte)214;
        public const byte FixExt8 = (byte)215;
        public const byte Float32 = (byte)202;
        public const byte Float64 = (byte)203;
        public const byte Int16 = (byte)209;
        public const byte Int32 = (byte)210;
        public const byte Int64 = (byte)211;
        public const byte Int8 = (byte)208;
        public const byte Map16 = (byte)222;
        public const byte Map32 = (byte)223;
        public const byte MaxFixArray = (byte)159;
        public const byte MaxFixInt = (byte)127;
        public const byte MaxFixMap = (byte)143;
        public const byte MaxFixStr = (byte)191;
        public const byte MaxNegativeFixInt = (byte)255;
        public const byte MinFixArray = (byte)144;
        public const byte MinFixInt = (byte)0;
        public const byte MinFixMap = (byte)128;
        public const byte MinFixStr = (byte)160;
        public const byte MinNegativeFixInt = (byte)224;
        public const byte NeverUsed = (byte)193;
        public const byte Nil = (byte)192;
        public const byte Str16 = (byte)218;
        public const byte Str32 = (byte)219;
        public const byte Str8 = (byte)217;
        public const byte True = (byte)195;
        public const byte UInt16 = (byte)205;
        public const byte UInt32 = (byte)206;
        public const byte UInt64 = (byte)207;
        public const byte UInt8 = (byte)204;
        public static bool IsSignedInteger(byte code) { throw null; }
        public static string ToFormatName(byte code) { throw null; }
        public static MessagePack.MessagePackType ToMessagePackType(byte code) { throw null; }
    }
    public static partial class MessagePackRange
    {
        public const int MaxFixArrayCount = 15;
        public const int MaxFixMapCount = 15;
        public const int MaxFixNegativeInt = -1;
        public const int MaxFixPositiveInt = 127;
        public const int MaxFixStringLength = 31;
        public const int MinFixNegativeInt = -32;
        public const int MinFixStringLength = 0;
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public ref partial struct MessagePackReader
    {
        private object _dummy;
        public MessagePackReader(System.Buffers.ReadOnlySequence<byte> readOnlySequence) { throw null; }
        public MessagePackReader(System.ReadOnlyMemory<byte> memory) { throw null; }
        public long Consumed { get { throw null; } }
        public bool End { get { throw null; } }
        public bool IsNil { get { throw null; } }
        public byte NextCode { get { throw null; } }
        public MessagePack.MessagePackType NextMessagePackType { get { throw null; } }
        public System.SequencePosition Position { get { throw null; } }
        public System.Buffers.ReadOnlySequence<byte> Sequence { get { throw null; } }
        public MessagePack.MessagePackReader Clone(System.Buffers.ReadOnlySequence<byte> readOnlySequence) { throw null; }
        public MessagePack.MessagePackReader CreatePeekReader() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public int ReadArrayHeader() { throw null; }
        public bool ReadBoolean() { throw null; }
        public byte ReadByte() { throw null; }
        public System.Buffers.ReadOnlySequence<byte> ReadBytes() { throw null; }
        public char ReadChar() { throw null; }
        public System.DateTime ReadDateTime() { throw null; }
        public double ReadDouble() { throw null; }
        public MessagePack.ExtensionResult ReadExtensionFormat() { throw null; }
        public MessagePack.ExtensionHeader ReadExtensionFormatHeader() { throw null; }
        public short ReadInt16() { throw null; }
        public int ReadInt32() { throw null; }
        public long ReadInt64() { throw null; }
        public int ReadMapHeader() { throw null; }
        public MessagePack.Nil ReadNil() { throw null; }
        public System.Buffers.ReadOnlySequence<byte> ReadRaw(long length) { throw null; }
        public sbyte ReadSByte() { throw null; }
        public float ReadSingle() { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public string ReadString() { throw null; }
        public System.Buffers.ReadOnlySequence<byte> ReadStringSegment() { throw null; }
        public ushort ReadUInt16() { throw null; }
        public uint ReadUInt32() { throw null; }
        public ulong ReadUInt64() { throw null; }
        public void Skip() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public bool TryReadNil() { throw null; }
    }
    public enum MessagePackType : byte
    {
        Array = (byte)7,
        Binary = (byte)6,
        Boolean = (byte)3,
        Extension = (byte)9,
        Float = (byte)4,
        Integer = (byte)1,
        Map = (byte)8,
        Nil = (byte)2,
        String = (byte)5,
        Unknown = (byte)0,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public ref partial struct MessagePackWriter
    {
        private object _dummy;
        private int _dummyPrimitive;
        public MessagePackWriter(System.Buffers.IBufferWriter<byte> writer) { throw null; }
        public bool OldSpec { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public MessagePack.MessagePackWriter Clone(System.Buffers.IBufferWriter<byte> writer) { throw null; }
        public void Flush() { }
        public void Write(bool value) { }
        public void Write(System.Buffers.ReadOnlySequence<byte> src) { }
        public void Write(byte value) { }
        public void Write(char value) { }
        public void Write(System.DateTime dateTime) { }
        public void Write(double value) { }
        public void Write(short value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Write(int value) { }
        public void Write(long value) { }
        public void Write(System.ReadOnlySpan<byte> src) { }
        public void Write(System.ReadOnlySpan<char> value) { }
        public void Write(sbyte value) { }
        public void Write(float value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Write(string value) { }
        public void Write(ushort value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Write(uint value) { }
        public void Write(ulong value) { }
        public void WriteArrayHeader(int count) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void WriteArrayHeader(uint count) { }
        public void WriteExtensionFormat(MessagePack.ExtensionResult extensionData) { }
        public void WriteExtensionFormatHeader(MessagePack.ExtensionHeader extensionHeader) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)][System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void WriteFixedArrayHeaderUnsafe(uint count) { }
        public void WriteInt16(short value) { }
        public void WriteInt32(int value) { }
        public void WriteInt64(long value) { }
        public void WriteInt8(sbyte value) { }
        public void WriteMapHeader(int count) { }
        public void WriteMapHeader(uint count) { }
        public void WriteNil() { }
        public void WriteRaw(System.Buffers.ReadOnlySequence<byte> rawMessagePackBlock) { }
        public void WriteRaw(System.ReadOnlySpan<byte> rawMessagePackBlock) { }
        public void WriteString(System.Buffers.ReadOnlySequence<byte> utf8stringBytes) { }
        public void WriteString(System.ReadOnlySpan<byte> utf8stringBytes) { }
        public void WriteUInt16(ushort value) { }
        public void WriteUInt32(uint value) { }
        public void WriteUInt64(ulong value) { }
        public void WriteUInt8(byte value) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, Size=1)]
    public partial struct Nil : System.IEquatable<MessagePack.Nil>
    {
        public static readonly MessagePack.Nil Default;
        public bool Equals(MessagePack.Nil other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class ReservedMessagePackExtensionTypeCode
    {
        public const sbyte DateTime = (sbyte)-1;
    }
}
namespace Microsoft.AspNetCore.Builder
{
    public static partial class ComponentEndpointConventionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder AddComponent(this Microsoft.AspNetCore.Builder.IEndpointConventionBuilder builder, System.Type componentType, string selector) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder AddComponent<TComponent>(this Microsoft.AspNetCore.Builder.IEndpointConventionBuilder builder, string selector) { throw null; }
    }
    public static partial class ComponentEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapBlazorHub(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type componentType, string selector, string path) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapBlazorHub<TComponent>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string selector, string path) where TComponent : Microsoft.AspNetCore.Components.IComponent { throw null; }
    }
}
namespace Microsoft.AspNetCore.Components.Browser.Rendering
{
    public partial class RemoteRendererException : System.Exception
    {
        public RemoteRendererException(string message) { }
    }
}
namespace Microsoft.AspNetCore.Components.Server
{
    public partial class CircuitOptions
    {
        public CircuitOptions() { }
        public System.TimeSpan DisconnectedCircuitRetentionPeriod { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxRetainedDisconnectedCircuits { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class ComponentHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public ComponentHub(System.IServiceProvider services, Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.ComponentHub> logger) { }
        public static Microsoft.AspNetCore.Http.PathString DefaultPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void BeginInvokeDotNetFromJS(string callId, string assemblyName, string methodIdentifier, long dotNetObjectId, string argsJson) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> ConnectCircuit(string circuitId) { throw null; }
        public override System.Threading.Tasks.Task OnDisconnectedAsync(System.Exception exception) { throw null; }
        public void OnRenderCompleted(long renderId, string errorMessageOrNull) { }
        public string StartCircuit(string uriAbsolute, string baseUriAbsolute) { throw null; }
    }
    public partial class ComponentPrerenderingContext
    {
        public ComponentPrerenderingContext() { }
        public System.Type ComponentType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.HttpContext Context { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Components.ParameterCollection Parameters { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public sealed partial class ComponentPrerenderResult
    {
        internal ComponentPrerenderResult() { }
        public void WriteTo(System.IO.TextWriter writer) { }
    }
    public partial interface IComponentPrerenderer
    {
        System.Threading.Tasks.Task<Microsoft.AspNetCore.Components.Server.ComponentPrerenderResult> PrerenderComponentAsync(Microsoft.AspNetCore.Components.Server.ComponentPrerenderingContext context);
    }
    public static partial class WasmMediaTypeNames
    {
        public static partial class Application
        {
            public const string Wasm = "application/wasm";
        }
    }
}
namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public sealed partial class Circuit
    {
        internal Circuit() { }
        public string Id { get { throw null; } }
    }
    public abstract partial class CircuitHandler
    {
        protected CircuitHandler() { }
        public virtual int Order { get { throw null; } }
        public virtual System.Threading.Tasks.Task OnCircuitClosedAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnCircuitOpenedAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnConnectionDownAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task OnConnectionUpAsync(Microsoft.AspNetCore.Components.Server.Circuits.Circuit circuit, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class RemoteUriHelper : Microsoft.AspNetCore.Components.UriHelperBase
    {
        public RemoteUriHelper(Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Components.Server.Circuits.RemoteUriHelper> logger) { }
        public bool HasAttachedJSRuntime { get { throw null; } }
        public override void InitializeState(string uriAbsolute, string baseUriAbsolute) { }
        protected override void NavigateToCore(string uri, bool forceLoad) { }
        [Microsoft.JSInterop.JSInvokableAttribute("NotifyLocationChanged")]
        public static void NotifyLocationChanged(string uriAbsolute) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ComponentServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddServerSideBlazor(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
