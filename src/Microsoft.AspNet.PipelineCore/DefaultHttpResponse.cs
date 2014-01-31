using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpResponse : HttpResponse
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;
        private FeatureReference<IHttpResponseInformation> _response = FeatureReference<IHttpResponseInformation>.Default;

        public DefaultHttpResponse(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpResponseInformation HttpResponseInformation
        {
            get { return _response.Fetch(_features); }
        }

        public override HttpContext HttpContext { get { return _context; } }

        public override int StatusCode
        {
            get { return HttpResponseInformation.StatusCode; }
            set { HttpResponseInformation.StatusCode = value; }
        }

        public override Stream Body
        {
            get { return HttpResponseInformation.Body; }
            set { HttpResponseInformation.Body = value; }
        }

        public override string ContentType
        {
            get
            {
                var contentTypeValues = HttpResponseInformation.Headers["Content-Type"];
                return contentTypeValues.Length == 0 ? null : contentTypeValues[0];
            }
            set
            {
                HttpResponseInformation.Headers["Content-Type"] = new[] { value };
            }
        }

        public override Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}