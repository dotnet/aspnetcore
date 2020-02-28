// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public partial class HubException : System.Exception
    {
        public HubException() { }
        public HubException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public HubException(string message) { }
        public HubException(string message, System.Exception innerException) { }
    }
    public partial interface IInvocationBinder
    {
        System.Collections.Generic.IReadOnlyList<System.Type> GetParameterTypes(string methodName);
        System.Type GetReturnType(string invocationId);
        System.Type GetStreamItemType(string streamId);
    }
    public partial interface ISignalRBuilder
    {
        Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
    }
}
namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public partial class CancelInvocationMessage : Microsoft.AspNetCore.SignalR.Protocol.HubInvocationMessage
    {
        public CancelInvocationMessage(string invocationId) : base (default(string)) { }
    }
    public partial class CloseMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        public static readonly Microsoft.AspNetCore.SignalR.Protocol.CloseMessage Empty;
        public CloseMessage(string error) { }
        public CloseMessage(string error, bool allowReconnect) { }
        public bool AllowReconnect { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class CompletionMessage : Microsoft.AspNetCore.SignalR.Protocol.HubInvocationMessage
    {
        public CompletionMessage(string invocationId, string error, object result, bool hasResult) : base (default(string)) { }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool HasResult { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public object Result { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage Empty(string invocationId) { throw null; }
        public override string ToString() { throw null; }
        public static Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage WithError(string invocationId, string error) { throw null; }
        public static Microsoft.AspNetCore.SignalR.Protocol.CompletionMessage WithResult(string invocationId, object payload) { throw null; }
    }
    public static partial class HandshakeProtocol
    {
        public static System.ReadOnlySpan<byte> GetSuccessfulHandshake(Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol protocol) { throw null; }
        public static bool TryParseRequestMessage(ref System.Buffers.ReadOnlySequence<byte> buffer, out Microsoft.AspNetCore.SignalR.Protocol.HandshakeRequestMessage requestMessage) { throw null; }
        public static bool TryParseResponseMessage(ref System.Buffers.ReadOnlySequence<byte> buffer, out Microsoft.AspNetCore.SignalR.Protocol.HandshakeResponseMessage responseMessage) { throw null; }
        public static void WriteRequestMessage(Microsoft.AspNetCore.SignalR.Protocol.HandshakeRequestMessage requestMessage, System.Buffers.IBufferWriter<byte> output) { }
        public static void WriteResponseMessage(Microsoft.AspNetCore.SignalR.Protocol.HandshakeResponseMessage responseMessage, System.Buffers.IBufferWriter<byte> output) { }
    }
    public partial class HandshakeRequestMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        public HandshakeRequestMessage(string protocol, int version) { }
        public string Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class HandshakeResponseMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        public static readonly Microsoft.AspNetCore.SignalR.Protocol.HandshakeResponseMessage Empty;
        public HandshakeResponseMessage(string error) { }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class HubInvocationMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        protected HubInvocationMessage(string invocationId) { }
        public System.Collections.Generic.IDictionary<string, string> Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string InvocationId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public abstract partial class HubMessage
    {
        protected HubMessage() { }
    }
    public abstract partial class HubMethodInvocationMessage : Microsoft.AspNetCore.SignalR.Protocol.HubInvocationMessage
    {
        protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments) : base (default(string)) { }
        protected HubMethodInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds) : base (default(string)) { }
        public object[] Arguments { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string[] StreamIds { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Target { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public static partial class HubProtocolConstants
    {
        public const int CancelInvocationMessageType = 5;
        public const int CloseMessageType = 7;
        public const int CompletionMessageType = 3;
        public const int InvocationMessageType = 1;
        public const int PingMessageType = 6;
        public const int StreamInvocationMessageType = 4;
        public const int StreamItemMessageType = 2;
    }
    public static partial class HubProtocolExtensions
    {
        public static byte[] GetMessageBytes(this Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol hubProtocol, Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { throw null; }
    }
    public partial interface IHubProtocol
    {
        string Name { get; }
        Microsoft.AspNetCore.Connections.TransferFormat TransferFormat { get; }
        int Version { get; }
        System.ReadOnlyMemory<byte> GetMessageBytes(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message);
        bool IsVersionSupported(int version);
        bool TryParseMessage(ref System.Buffers.ReadOnlySequence<byte> input, Microsoft.AspNetCore.SignalR.IInvocationBinder binder, out Microsoft.AspNetCore.SignalR.Protocol.HubMessage message);
        void WriteMessage(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Buffers.IBufferWriter<byte> output);
    }
    public partial class InvocationBindingFailureMessage : Microsoft.AspNetCore.SignalR.Protocol.HubInvocationMessage
    {
        public InvocationBindingFailureMessage(string invocationId, string target, System.Runtime.ExceptionServices.ExceptionDispatchInfo bindingFailure) : base (default(string)) { }
        public System.Runtime.ExceptionServices.ExceptionDispatchInfo BindingFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Target { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class InvocationMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMethodInvocationMessage
    {
        public InvocationMessage(string target, object[] arguments) : base (default(string), default(string), default(object[]), default(string[])) { }
        public InvocationMessage(string invocationId, string target, object[] arguments) : base (default(string), default(string), default(object[]), default(string[])) { }
        public InvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds) : base (default(string), default(string), default(object[]), default(string[])) { }
        public override string ToString() { throw null; }
    }
    public partial class PingMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        internal PingMessage() { }
        public static readonly Microsoft.AspNetCore.SignalR.Protocol.PingMessage Instance;
    }
    public partial class StreamBindingFailureMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMessage
    {
        public StreamBindingFailureMessage(string id, System.Runtime.ExceptionServices.ExceptionDispatchInfo bindingFailure) { }
        public System.Runtime.ExceptionServices.ExceptionDispatchInfo BindingFailure { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Id { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class StreamInvocationMessage : Microsoft.AspNetCore.SignalR.Protocol.HubMethodInvocationMessage
    {
        public StreamInvocationMessage(string invocationId, string target, object[] arguments) : base (default(string), default(string), default(object[]), default(string[])) { }
        public StreamInvocationMessage(string invocationId, string target, object[] arguments, string[] streamIds) : base (default(string), default(string), default(object[]), default(string[])) { }
        public override string ToString() { throw null; }
    }
    public partial class StreamItemMessage : Microsoft.AspNetCore.SignalR.Protocol.HubInvocationMessage
    {
        public StreamItemMessage(string invocationId, object item) : base (default(string)) { }
        public object Item { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public override string ToString() { throw null; }
    }
}
