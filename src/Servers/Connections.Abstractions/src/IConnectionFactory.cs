using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionFactory
    {
        ValueTask<ConnectionContext> ConnectAsync(System.Net.EndPoint endpoint, CancellationToken cancellationToken = default);
    }
}
