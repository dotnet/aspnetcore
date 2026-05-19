// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

internal sealed class SslDuplexPipe : DuplexPipeStreamAdapter<SslStream>
{
    public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions)
        : this(transport, readerOptions, writerOptions, s => new SslStream(s))
    {
    }

    public SslDuplexPipe(IDuplexPipe transport, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, SslStream> factory) :
        base(transport, readerOptions, writerOptions, factory)
    {
    }
}
