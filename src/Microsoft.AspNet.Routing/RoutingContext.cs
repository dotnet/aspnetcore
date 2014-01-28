
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class RoutingContext
    {
        public RoutingContext(IDictionary<string, object> context)
        {
            this.Context = context;

            this.RequestMethod = (string)context["owin.RequestMethod"];
            this.RequestPath = (string)context["owin.RequestPath"];
        }

        public IDictionary<string, object> Context
        {
            get;
            private set;
        }

        public string RequestMethod
        {
            get;
            private set;
        }

        public string RequestPath
        {
            get;
            private set;
        }
    }
}
