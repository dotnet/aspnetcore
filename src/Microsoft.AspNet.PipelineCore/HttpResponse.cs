using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpEnvironment;
using Microsoft.AspNet.Interfaces;

namespace Microsoft.AspNet.PipelineCore
{
    public class HttpResponse : HttpResponseBase
    {
        private readonly HttpContext _context;
        private IHttpResponse _response;
        private int _revision;

        public HttpResponse(HttpContext context)
        {
            _context = context;
        }

        private IHttpResponse IHttpResponse
        {
            get { return EnsureCurrent(_response) ?? (_response = _context.GetFeature<IHttpResponse>()); }
        }

        private T EnsureCurrent<T>(T feature) where T : class
        {
            if (_revision == _context.Revision) return feature;

            _response = null;
            _revision = _context.Revision;
            return null;
        } 

        public override HttpContextBase HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return IHttpResponse.StatusCode; }
            set { IHttpResponse.StatusCode = value; }
        }

        public override Stream Body { get { return _response.Body; } set { _response.Body = value; } }
    }
}