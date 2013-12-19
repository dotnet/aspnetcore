using System;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpEnvironment;

namespace Microsoft.AspNet.PipelineCore
{
    public class HttpContext : HttpContextBase, IHttpEnvironment
    {
        private readonly IFeatureContainer _features;
        private readonly HttpRequestBase _request;
        private readonly HttpResponseBase _response;

        public HttpContext(IHttpEnvironment environment)
        {
            _features = environment;
            _request = new HttpRequest(this);
            _response = new HttpResponse(this);
        }

        public override HttpRequestBase Request { get { return _request; } }
        public override HttpResponseBase Response { get { return _response; } }

        public void Dispose()
        {
            _features.Dispose();
        }

        public object GetFeature(Type type)
        {
            return _features.GetFeature(type);
        }

        public void SetFeature(Type type, object feature)
        {
            _features.SetFeature(type, feature);
        }

        public int Revision
        {
            get { return _features.Revision; }
        }
    }
}
