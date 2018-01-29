using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Pipelines
{
    public class PipeConnection : IDuplexPipe
    {
        public PipeConnection(PipeReader reader, PipeWriter writer)
        {
            Input = reader;
            Output = writer;
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        public void Dispose()
        {
        }
    }
}
