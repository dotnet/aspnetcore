using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionListener
    {
        EndPoint EndPoint { get; }

        ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default);

        ValueTask UnbindAsync(CancellationToken cancellationToken = default);

        ValueTask DisposeAsync();
    }
}
