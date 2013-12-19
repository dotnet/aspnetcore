using System;

namespace Microsoft.AspNet.Abstractions
{
    public interface IBuilder 
    {
        IBuilder Use(object middleware, params object[] args);

        IBuilder New();
        RequestDelegate Build();

        object GetFeature(Type type);    
        void SetFeature(Type type, object feature);
    }
}
