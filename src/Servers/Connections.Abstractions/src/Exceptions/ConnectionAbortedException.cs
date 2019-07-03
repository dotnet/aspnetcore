using System;
using System.Threading;

namespace Microsoft.AspNetCore.Connections
{
    public class ConnectionAbortedException : OperationCanceledException
    {
        public ConnectionAbortedException() :
            this("The connection was aborted")
        {

        }

        public ConnectionAbortedException(string message)
            : base(message)
        {
        }

        public ConnectionAbortedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public ConnectionAbortedException(string message, Exception inner, CancellationToken cancellationToken)
            : base(message, inner, cancellationToken)
        {
        }
    }
}
