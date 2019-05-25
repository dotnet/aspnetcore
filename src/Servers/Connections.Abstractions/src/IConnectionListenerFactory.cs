using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionListenerFactory
    {
        // Add CancellationToken cancellationToken = default
        ValueTask<IConnectionListener> BindAsync(EndPoint endpoint);
    }
}
