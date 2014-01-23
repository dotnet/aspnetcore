using System;

namespace Microsoft.AspNet.Abstractions
{
    public interface IBuilder 
    {
        IBuilder Use(object middleware, params object[] args);

        IBuilder New();
        RequestDelegate Build();

        object GetItem(Type type);    
        void SetItem(Type type, object feature);
    }
}
