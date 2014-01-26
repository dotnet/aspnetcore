#if NET45
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.Owin;

namespace MvcSample
{
    public class OwinHttpContext : HttpContext
    {
        private readonly IOwinContext _context;
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        public OwinHttpContext(IOwinContext context)
        {
            _context = context;
            _request = new OwinHttpRequest(this, context.Request);
            _response = new OwinHttpResponse(this, context.Response);
        }

        public override void Dispose()
        {

        }

        public override object GetInterface(Type type)
        {
            return null;
        }

        public override HttpRequest Request
        {
            get { return _request; }
        }

        public override HttpResponse Response
        {
            get { return _response; }
        }

        public override void SetInterface(Type type, object instance)
        {

        }

        private class OwinHttpRequest : HttpRequest
        {
            private HttpContext _context;
            private IOwinRequest _request;

            public OwinHttpRequest(HttpContext context, IOwinRequest request)
            {
                _context = context;
                _request = request;
            }

            public override Stream Body
            {
                get
                {
                    return _request.Body;
                }
                set
                {
                    _request.Body = value;
                }
            }

            public override HttpContext HttpContext
            {
                get { return _context; }
            }

            public override Microsoft.AspNet.Abstractions.PathString Path
            {
                get
                {
                    return new Microsoft.AspNet.Abstractions.PathString(_request.Path.Value);
                }
                set
                {
                    _request.Path = new Microsoft.Owin.PathString(value.Value);
                }
            }

            public override Microsoft.AspNet.Abstractions.PathString PathBase
            {
                get
                {
                    return new Microsoft.AspNet.Abstractions.PathString(_request.PathBase.Value);
                }
                set
                {
                    _request.PathBase = new Microsoft.Owin.PathString(value.Value);
                }
            }

            public override Microsoft.AspNet.Abstractions.QueryString QueryString
            {
                get
                {
                    return new Microsoft.AspNet.Abstractions.QueryString(_request.QueryString.Value);
                }
                set
                {
                    _request.QueryString = new Microsoft.Owin.QueryString(value.Value);
                }
            }

            public override Uri Uri
            {
                get { return _request.Uri; }
            }
        }

        private class OwinHttpResponse : HttpResponse
        {
            private readonly HttpContext _context;
            private readonly IOwinResponse _response;

            public OwinHttpResponse(HttpContext context, IOwinResponse response)
            {
                _context = context;
                _response = response;
            }

            public override Stream Body
            {
                get
                {
                    return _response.Body;
                }
                set
                {
                    _response.Body = value;
                }
            }

            public override string ContentType
            {
                get
                {
                    return _response.ContentType;
                }
                set
                {
                    _response.ContentType = value;
                }
            }

            public override HttpContext HttpContext
            {
                get { return _context; }
            }

            public override int StatusCode
            {
                get
                {
                    return _response.StatusCode;
                }
                set
                {
                    _response.StatusCode = value;
                }
            }

            public override Task WriteAsync(string data)
            {
                return _response.WriteAsync(data);
            }
        }
    }
}
#endif