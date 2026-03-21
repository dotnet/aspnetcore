// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines;

/// <remarks>
/// A <see cref="Pipe"/> is a reader-writer pair, where content written to the writer can be read from the reader.
///
/// An <see cref="IDuplexPipe"/> is *not* a pipe.  It is also a reader-writer pair, but the reader and writer are not
/// connected.  Rather, it can be regarded as *one end* of a two-way (i.e. duplex) communication channel, where content
/// written to the writer is sent to the counterparty and content received from the counterparty is readable from the
/// reader.
///
/// A <see cref="DuplexPipePair"/> is a pair of <see cref="IDuplexPipe"/> instances, each of which represents one end of
/// a two-way communication channel.  (In a sense, this makes it a "duplex pipe".)  It can also be viewed as a pair of
/// <see cref="Pipe"/>s, as these underlie the <see cref="IDuplexPipe"/> instances.  In either view, it is composed of
/// two <see cref="PipeReader"/>s and two <see cref="PipeWriter"/>s - it is only how they are grouped that differs.
/// </remarks>
internal sealed class DuplexPipe : IDuplexPipe
{
    public DuplexPipe(PipeReader reader, PipeWriter writer)
    {
        Input = reader;
        Output = writer;
    }

    public PipeReader Input { get; }

    public PipeWriter Output { get; }

    public static DuplexPipePair CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
    {
        var input = new Pipe(inputOptions);
        var output = new Pipe(outputOptions);

        var transportToApplication = new DuplexPipe(output.Reader, input.Writer);
        var applicationToTransport = new DuplexPipe(input.Reader, output.Writer);

        return new DuplexPipePair(applicationToTransport, transportToApplication);
    }

    // This class exists to work around issues with value tuple on .NET Framework
    public readonly struct DuplexPipePair
    {
        public IDuplexPipe Transport { get; }
        public IDuplexPipe Application { get; }

        public DuplexPipePair(IDuplexPipe transport, IDuplexPipe application)
        {
            Transport = transport;
            Application = application;
        }
    }
}
