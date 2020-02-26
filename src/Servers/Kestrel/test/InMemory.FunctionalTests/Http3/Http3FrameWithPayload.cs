using System;
using System.Buffers;
using System.Net.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        internal class Http3FrameWithPayload : Http3RawFrame
        {
            public Http3FrameWithPayload() : base()
            {
            }

            // This does not contain extended headers
            public Memory<byte> Payload { get; set; }

            public ReadOnlySequence<byte> PayloadSequence => new ReadOnlySequence<byte>(Payload);
        }
    }
}
