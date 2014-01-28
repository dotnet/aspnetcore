
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if NET45
using Owin;
#endif

namespace Microsoft.AspNet.Routing
{
    public interface IRouteBuilder
    {
#if NET45
        IAppBuilder AppBuilder
        {
            get;
        }
#endif
        IRouteEngine Engine
        {
            get;
        }

        IRouteEndpoint ForApp(Func<Func<IDictionary<string, object>, Task>> handlerFactory);
    }
}

