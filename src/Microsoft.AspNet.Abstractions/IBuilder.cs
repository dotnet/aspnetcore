using System;

namespace Microsoft.AspNet.Abstractions
{
    public interface IBuilder
    {
        IServiceProvider ServiceProvider { get; set; }
        IServerInformation Server { get; set; }

        IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
        IBuilder Run(RequestDelegate handler);

        IBuilder New();
        RequestDelegate Build();
    }
}
