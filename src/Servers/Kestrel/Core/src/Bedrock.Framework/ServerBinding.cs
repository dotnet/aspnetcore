using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Bedrock.Framework
{
    public abstract class ServerBinding
    {
        public virtual ConnectionDelegate Application { get; protected set; }

        public abstract IAsyncEnumerable<IConnectionListener> BindAsync(CancellationToken cancellationToken = default);
    }
}
