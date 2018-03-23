using System;

namespace Microsoft.AspNetCore.Connections
{
    public interface IConnectionBuilder
    {
        IServiceProvider ApplicationServices { get; }

        IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware);

        ConnectionDelegate Build();
    }
}
