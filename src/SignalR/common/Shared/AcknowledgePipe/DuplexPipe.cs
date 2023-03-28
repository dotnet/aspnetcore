using System.IO.Pipelines;

namespace PipelinesOverNetwork
{
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

    internal sealed class AckDuplexPipe : IDuplexPipe
    {
        
        public AckDuplexPipe(PipeReader reader, PipeWriter writer)
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

            // wire up both sides for testing
            var ackWriterApp = new AckPipeWriter(output.Writer);
            var ackReaderApp = new AckPipeReader(output.Reader);
            var ackWriterClient = new AckPipeWriter(input.Writer);
            var ackReaderClient = new AckPipeReader(input.Reader);
            var transportReader = new ParseAckPipeReader(input.Reader, ackWriterApp, ackReaderApp);
            var applicationReader = new ParseAckPipeReader(ackReaderApp, ackWriterClient, ackReaderClient);
            var transportToApplication = new AckDuplexPipe(applicationReader, ackWriterClient);
            var applicationToTransport = new AckDuplexPipe(transportReader, ackWriterApp);

            // Use for one side only, i.e. server
            //var ackWriter = new AckPipeWriter(output.Writer);
            //var ackReader = new AckPipeReader(output.Reader);
            //var transportReader = new ParseAckPipeReader(input.Reader, ackWriter, ackReader);
            //var transportToApplication = new DuplexPipe(ackReader, input.Writer);
            //var applicationToTransport = new DuplexPipe(transportReader, ackWriter);

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
}
