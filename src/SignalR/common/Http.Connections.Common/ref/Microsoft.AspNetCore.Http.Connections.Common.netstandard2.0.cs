// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Connections
{
    public partial class AvailableTransport
    {
        public AvailableTransport() { }
        public System.Collections.Generic.IList<string> TransferFormats { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public static partial class HttpTransports
    {
        public static readonly Microsoft.AspNetCore.Http.Connections.HttpTransportType All;
    }
    [System.FlagsAttribute]
    public enum HttpTransportType
    {
        None = 0,
        WebSockets = 1,
        ServerSentEvents = 2,
        LongPolling = 4,
    }
    public static partial class NegotiateProtocol
    {
        [System.ObsoleteAttribute("This method is obsolete and will be removed in a future version. The recommended alternative is ParseResponse(ReadOnlySpan{byte}).")]
        public static Microsoft.AspNetCore.Http.Connections.NegotiationResponse ParseResponse(System.IO.Stream content) { throw null; }
        public static Microsoft.AspNetCore.Http.Connections.NegotiationResponse ParseResponse(System.ReadOnlySpan<byte> content) { throw null; }
        public static void WriteResponse(Microsoft.AspNetCore.Http.Connections.NegotiationResponse response, System.Buffers.IBufferWriter<byte> output) { }
    }
    public partial class NegotiationResponse
    {
        public NegotiationResponse() { }
        public string AccessToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Collections.Generic.IList<Microsoft.AspNetCore.Http.Connections.AvailableTransport> AvailableTransports { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ConnectionToken { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Url { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
