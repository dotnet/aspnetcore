// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SignalR
{
    public partial class MessagePackHubProtocolOptions
    {
        public MessagePackHubProtocolOptions() { }
        public System.Collections.Generic.IList<MessagePack.IFormatterResolver> FormatterResolvers { get { throw null; } set { } }
    }
}
namespace Microsoft.AspNetCore.SignalR.Protocol
{
    public partial class MessagePackHubProtocol : Microsoft.AspNetCore.SignalR.Protocol.IHubProtocol
    {
        public MessagePackHubProtocol() { }
        public MessagePackHubProtocol(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.SignalR.MessagePackHubProtocolOptions> options) { }
        public string Name { get { throw null; } }
        public Microsoft.AspNetCore.Connections.TransferFormat TransferFormat { get { throw null; } }
        public int Version { get { throw null; } }
        public System.ReadOnlyMemory<byte> GetMessageBytes(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { throw null; }
        public bool IsVersionSupported(int version) { throw null; }
        public bool TryParseMessage(ref System.Buffers.ReadOnlySequence<byte> input, Microsoft.AspNetCore.SignalR.IInvocationBinder binder, out Microsoft.AspNetCore.SignalR.Protocol.HubMessage message) { throw null; }
        public void WriteMessage(Microsoft.AspNetCore.SignalR.Protocol.HubMessage message, System.Buffers.IBufferWriter<byte> output) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class MessagePackProtocolDependencyInjectionExtensions
    {
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder) where TBuilder : Microsoft.AspNetCore.SignalR.ISignalRBuilder { throw null; }
        public static TBuilder AddMessagePackProtocol<TBuilder>(this TBuilder builder, System.Action<Microsoft.AspNetCore.SignalR.MessagePackHubProtocolOptions> configure) where TBuilder : Microsoft.AspNetCore.SignalR.ISignalRBuilder { throw null; }
    }
}
