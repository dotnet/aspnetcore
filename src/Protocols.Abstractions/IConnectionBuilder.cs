using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Protocols
{
    public interface IConnectionBuilder
    {
        IServiceProvider ApplicationServices { get; }

        IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

        ConnectionDelegate Build();
    }
}
