using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public interface IConstraint
    {
        bool MatchInbound(RoutingContext context, IDictionary<string, object> values, object value);

        bool MatchOutbound(RouteBindingContext context, IDictionary<string, object> values, object value);
    }
}
