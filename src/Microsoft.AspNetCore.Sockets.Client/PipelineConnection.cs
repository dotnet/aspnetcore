using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal class PipelineConnection : IPipelineConnection
    {
        public IPipelineReader Input { get; }
        public IPipelineWriter Output { get; }

        public PipelineConnection(IPipelineReader input, IPipelineWriter output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
        }
    }
}
