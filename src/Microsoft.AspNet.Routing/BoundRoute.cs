
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class BoundRoute
    {
        public BoundRoute(string url, IDictionary<string, object> values)
        {
            this.Url = url;
            this.Values = values;
        }

        public string Url
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
