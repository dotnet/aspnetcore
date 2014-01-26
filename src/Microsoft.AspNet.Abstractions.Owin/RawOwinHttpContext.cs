using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions.Owin
{
    public class RawOwinHttpContext : HttpContext
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        public RawOwinHttpContext(IDictionary<string, object> env)
        {
            _request = new RawOwinHttpRequest(this, env);
            _response = new RawOwinHttpResponse(this, env);
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

        private class RawOwinHttpRequest : HttpRequest
        {
            private HttpContext _context;
            private readonly IDictionary<string, object> _env;

            public RawOwinHttpRequest(HttpContext context, IDictionary<string, object> env)
            {
                _context = context;
                _env = env;
            }

            public override Stream Body
            {
                get
                {
                    return (Stream)_env["owin.ResponseBody"];
                }
                set
                {
                    _env["owin.ResponseBody"] = value;
                }
            }

            public override HttpContext HttpContext
            {
                get { return _context; }
            }

            public override PathString Path
            {
                get
                {
                    return new PathString((string)_env["owin.RequestPath"]);
                }
                set
                {
                    _env["owin.RequestPath"] = value.Value;
                }
            }

            public override PathString PathBase
            {
                get
                {
                    return new PathString((string)_env["owin.RequestPathBase"]);
                }
                set
                {
                    _env["owin.RequestPathBase"] = value.Value;
                }
            }

            public override QueryString QueryString
            {
                get
                {
                    return new QueryString((string)_env["owin.RequestQueryString"]);
                }
                set
                {
                    _env["owin.RequestQueryString"] = value.Value;
                }
            }

            public override Uri Uri
            {
                get
                {
                    // TODO: Implement this sometime
                    throw new NotImplementedException();
                }
            }
        }

        private class RawOwinHttpResponse : HttpResponse
        {
            private readonly HttpContext _context;
            private readonly IDictionary<string, object> _env;

            public RawOwinHttpResponse(HttpContext context, IDictionary<string, object> env)
            {
                _context = context;
                _env = env;
            }

            public override Stream Body
            {
                get
                {
                    return (Stream)_env["owin.ResponseBody"];
                }
                set
                {
                    _env["owin.ResponseBody"] = value;
                }
            }

            public override string ContentType
            {
                get
                {
                    return GetHeader("Content-Type");
                }
                set
                {
                    SetHeader("Content-Type", value);
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
                    return (int)_env["owin.ResponseStatusCode"];
                }
                set
                {
                    _env["owin.ResponseStatusCode"] = value;
                }
            }

            public override Task WriteAsync(string data)
            {
                var bytes = Encoding.UTF8.GetBytes(data);

                return Body.WriteAsync(bytes, 0, bytes.Length);
            }

            private void SetHeader(string name, string value)
            {
                var headers = (IDictionary<string, string[]>)_env["owin.ResponseHeaders"];

                headers[name] = new[] { value };
            }

            private string GetHeader(string name)
            {
                var headers = (IDictionary<string, string[]>)_env["owin.ResponseHeaders"];

                string[] values;
                if (headers.TryGetValue(name, out values) && values.Length > 0)
                {
                    return values[0];
                }

                return null;
            }
        }
    }
}
