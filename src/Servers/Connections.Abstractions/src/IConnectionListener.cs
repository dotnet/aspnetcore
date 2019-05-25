using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionListener
    {
        EndPoint EndPoint { get; }

        ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default);

        // Rename to UnbindAsync maybe... or StopListeningAsync?
        ValueTask StopAsync(CancellationToken cancellationToken = default);

        ValueTask DisposeAsync();
    }
}
