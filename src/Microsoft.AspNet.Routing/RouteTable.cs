
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing
{
    public class RouteTable
    {
        public RouteTable()
        {
            this.Routes = new List<IRoute>();

            this.NamedRoutes = new Dictionary<string, IRoute>(StringComparer.OrdinalIgnoreCase);
        }

        public List<IRoute> Routes
        {
            get;
            private set;
        }

        public IDictionary<string, IRoute> NamedRoutes
        {
            get;
            private set;
        }

        public void Add(IRoute route)
        {
            this.Add(null, route);
        }

        public void Add(string name, IRoute route)
        {
            this.Routes.Add(route);

            if (!String.IsNullOrEmpty(name))
            {
                this.NamedRoutes.Add(name, route);
            }
        }
    }
}
