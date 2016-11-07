using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubContext<THub, TClient>
    {
        IHubConnectionContext<TClient> Clients { get; }
    }

    public interface IHubContext<THub> : IHubContext<THub, IClientProxy>
    {
    }
}
