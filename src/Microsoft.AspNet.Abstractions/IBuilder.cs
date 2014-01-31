using System;

namespace Microsoft.AspNet.Abstractions
{
    public interface IBuilder
    {
        IBuilder Use(Func<RequestDelegate, RequestDelegate> middleware);
        IBuilder Run(RequestDelegate handler);

        IBuilder New();
        RequestDelegate Build();

        object GetItem(Type type);
        void SetItem(Type type, object feature);
    }
}
