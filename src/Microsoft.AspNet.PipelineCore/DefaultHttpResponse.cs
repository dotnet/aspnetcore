using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpResponse : HttpResponse
    {
        private readonly DefaultHttpContext _context;
        private IHttpResponseInformation _response;
        private int _revision;

        public DefaultHttpResponse(DefaultHttpContext context)
        {
            _context = context;
        }

        private IHttpResponseInformation IHttpResponse
        {
            get { return EnsureCurrent(_response) ?? (_response = _context.GetInterface<IHttpResponseInformation>()); }
        }

        private T EnsureCurrent<T>(T feature) where T : class
        {
            if (_revision == _context.Revision) return feature;

            _response = null;
            _revision = _context.Revision;
            return null;
        }

        public override HttpContext HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return IHttpResponse.StatusCode; }
            set { IHttpResponse.StatusCode = value; }
        }

        public override Stream Body { get { return _response.Body; } set { _response.Body = value; } }

        public override string ContentType
        {
            get
            {
                var contentTypeValues = IHttpResponse.Headers["Content-Type"];
                return contentTypeValues.Length == 0 ? null : contentTypeValues[0];
            }
            set
            {
                IHttpResponse.Headers["Content-Type"] = new[] { value };
            }
        }

        public override Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}