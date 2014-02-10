using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.PipelineCore;

namespace Microsoft.AspNet.Hosting.Builder
{
    public class HttpContextFactory : IHttpContextFactory
    {
        public HttpContext CreateHttpContext(object serverContext)
        {
            var featureObject = serverContext as IFeatureCollection ?? new FeatureObject(serverContext);
            var featureCollection = new FeatureCollection(featureObject);
            var httpContext = new DefaultHttpContext(featureCollection);
            return httpContext;
        }
    }
}