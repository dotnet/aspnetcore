using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.Routing
{
    public class RouteBindingContext
    {
        public RouteBindingContext(IDictionary<string, object> context, IDictionary<string, object> values)
        {
            this.Context = context;
            this.Values = values;

            this.AmbientValues = context.GetRouteMatchValues();
        }

        public IDictionary<string, object> AmbientValues
        {
            get;
            private set;
        }

        public IDictionary<string, object> Context
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
