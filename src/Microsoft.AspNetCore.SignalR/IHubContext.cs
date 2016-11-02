using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR
{
    public interface IHubContext<THub>
    {
        IHubConnectionContext Clients { get; }
    }
}
