using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    public class PipeConnection : IPipeConnection
    {
        public PipeConnection(IPipeReader reader, IPipeWriter writer)
        {
            Input = reader;
            Output = writer;
        }

        public IPipeReader Input { get; }

        public IPipeWriter Output { get; }

        public void Dispose()
        {
        }
    }
}
