
using System;

namespace Microsoft.AspNet.Routing.Tree
{
    public static class TreeRouteBuilderExtensions
    {
        public static ITreeRouteBuilder Path(this ITreeRouteBuilder routeBuilder, string path)
        {
            ITreeRouteBuilder current = routeBuilder;
            foreach (var segment in path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = current.Segment(() => new PathSegment(segment));
            }

            return current;
        }

        public static ITreeRouteBuilder Parameter(this ITreeRouteBuilder routeBuilder, string name)
        {
            return routeBuilder.Segment(() => new ParameterSegment(name));
        }
    }
}
