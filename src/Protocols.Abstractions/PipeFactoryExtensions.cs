namespace System.IO.Pipelines
{
    public static class PipeFactoryExtensions
    {
        public static (IPipeConnection Transport, IPipeConnection Application) CreateConnectionPair(this PipeFactory pipeFactory)
        {
            return pipeFactory.CreateConnectionPair(new PipeOptions(), new PipeOptions());
        }

        public static (IPipeConnection Transport, IPipeConnection Application) CreateConnectionPair(this PipeFactory pipeFactory, PipeOptions inputOptions, PipeOptions outputOptions)
        {
            var input = pipeFactory.Create(inputOptions);
            var output = pipeFactory.Create(outputOptions);

            var transportToApplication = new PipeConnection(output.Reader, input.Writer);
            var applicationToTransport = new PipeConnection(input.Reader, output.Writer);

            return (applicationToTransport, transportToApplication);
        }
    }
}
