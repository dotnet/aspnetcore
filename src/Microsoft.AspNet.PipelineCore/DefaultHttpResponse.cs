using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
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

        public override IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(HttpResponseInformation.Headers); }
        }

        public override Stream Body
        {
            get { return HttpResponseInformation.Body; }
            set { HttpResponseInformation.Body = value; }
        }

        public override long? ContentLength
        {
            get
            {
                return ParsingHelpers.GetContentLength(Headers);
            }
            set
            {
                ParsingHelpers.SetContentLength(Headers, value);
            }
        }

        public override string ContentType
        {
            get
            {
                var contentType = Headers[Constants.Headers.ContentType];
                return contentType;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    HttpResponseInformation.Headers.Remove(Constants.Headers.ContentType);
                }
                else
                {
                    HttpResponseInformation.Headers[Constants.Headers.ContentType] = new[] { value };
                }
            }
        }

        public override Task WriteAsync(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}