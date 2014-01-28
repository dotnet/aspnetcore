using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public class RouteMatch
    {
        public RouteMatch(Func<IDictionary<string, object>, Task> destination)
            : this(destination, null)
        {
        }

        public RouteMatch(Func<IDictionary<string, object>, Task> destination, IDictionary<string, object> values)
        {
            this.Destination = destination;
            this.Values = values;
        }

        public Func<IDictionary<string, object>, Task> Destination
        {
            get;
            private set;
        }

        public IDictionary<string, object> Values
        {
            get;
            private set;
        }
    }
}
