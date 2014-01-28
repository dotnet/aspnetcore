using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Tree
{
    public static class TreeRouteExtensions
    {
        public static ITreeRouteBuilder AddTreeRoute(this IRouteBuilder routeBuilder)
        {
            return new TreeRouteBuilder(routeBuilder);
        }
    }
}
