using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public class ConnectionAbortedException : OperationCanceledException
    {
        public ConnectionAbortedException() :
            this("The connection was aborted")
        {

        }

        public ConnectionAbortedException(string message) : base(message)
        {
        }

        public ConnectionAbortedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
