using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Tree
{
    internal class TreeRouteBuilder : ITreeRouteBuilder
    {
        public TreeRouteBuilder(IRouteBuilder routeBuilder)
        {
            this.RouteBuilder = routeBuilder;
        }

        public TreeRouteBuilder(Func<ITreeSegment> current, TreeRouteBuilder parent)
        {
            this.Current = current;
            this.Parent = parent;

            this.RouteBuilder = parent.RouteBuilder;
            this.RouteEndpoint = parent.RouteEndpoint;
        }

        private Func<ITreeSegment> Current
        {
            get;
            set;
        }

        private TreeRouteBuilder Parent
        {
            get;
            set;
        }

        private IRouteBuilder RouteBuilder
        {
            get;
            set;
        }

        private IRouteEndpoint RouteEndpoint
        {
            get;
            set;
        }

        public void Build()
        {
            throw new NotImplementedException();
        }

        public ITreeRouteBuilder Endpoint(IRouteEndpoint endpoint)
        {
            this.RouteEndpoint = endpoint;
            return this;
        }

        public ITreeRouteBuilder Segment(Func<ITreeSegment> segmentBuilder)
        {
            return new TreeRouteBuilder(segmentBuilder, this);
        }
    }
}
