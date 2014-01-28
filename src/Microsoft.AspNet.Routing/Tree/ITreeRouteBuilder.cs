using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Tree
{
    public interface ITreeRouteBuilder
    {
        void Build();

        ITreeRouteBuilder Endpoint(IRouteEndpoint endpoint);

        ITreeRouteBuilder Segment(Func<ITreeSegment> segmentBuilder);
    }
}
