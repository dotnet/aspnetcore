using System.Buffers;

namespace System.IO.Pipelines
{
    public static class PipeFactory
    {
        public static (IPipeConnection Transport, IPipeConnection Application) CreateConnectionPair(BufferPool memoryPool)
        {
            return CreateConnectionPair(new PipeOptions(memoryPool), new PipeOptions(memoryPool));
        }

        public static (IPipeConnection Transport, IPipeConnection Application) CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
        {
            var input = new Pipe(inputOptions);
            var output = new Pipe(outputOptions);

            var transportToApplication = new PipeConnection(output.Reader, input.Writer);
            var applicationToTransport = new PipeConnection(input.Reader, output.Writer);

            return (applicationToTransport, transportToApplication);
        }
    }
}
