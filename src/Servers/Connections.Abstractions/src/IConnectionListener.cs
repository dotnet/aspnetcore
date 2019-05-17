using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionListener
    {
        ValueTask<ConnectionContext> AcceptAsync();

        ValueTask DisposeAsync();
    }
}
