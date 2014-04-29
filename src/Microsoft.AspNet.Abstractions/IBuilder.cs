using System;

namespace Microsoft.AspNet.Abstractions
{
    public interface IBuilder
    {
        IServiceProvider ApplicationServices { get; set; }
        IServerInformation Server { get; set; }

        IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);

        IBuilder New();
        RequestDelegate Build();
    }
}
