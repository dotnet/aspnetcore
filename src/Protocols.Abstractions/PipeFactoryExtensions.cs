using System.Buffers;

namespace System.IO.Pipelines
{
    public static class PipeFactory
    {
        public static (IDuplexPipe Transport, IDuplexPipe Application) CreateConnectionPair(MemoryPool memoryPool)
        {
            return CreateConnectionPair(new PipeOptions(memoryPool), new PipeOptions(memoryPool));
        }

        public static (IDuplexPipe Transport, IDuplexPipe Application) CreateConnectionPair(PipeOptions inputOptions, PipeOptions outputOptions)
        {
            var input = new Pipe(inputOptions);
            var output = new Pipe(outputOptions);

            var transportToApplication = new PipeConnection(output.Reader, input.Writer);
            var applicationToTransport = new PipeConnection(input.Reader, output.Writer);

            return (applicationToTransport, transportToApplication);
        }
    }
}
