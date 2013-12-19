using System;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpEnvironment;
using Microsoft.AspNet.Interfaces;

namespace Microsoft.AspNet.PipelineCore
{
    public class HttpRequest : HttpRequestBase
    {
        private readonly HttpContext _context;
        private int _revision;
        private IHttpRequest _request;
        private IHttpConnection _connection;

        public HttpRequest(HttpContext context)
        {
            _context = context;
        }

        private IHttpRequest IHttpRequest
        {
            get { return EnsureCurrent(_request) ?? (_request = _context.GetFeature<IHttpRequest>()); }
        }

        private IHttpConnection IHttpConnection
        {
            get { return EnsureCurrent(_connection) ?? (_connection = _context.GetFeature<IHttpConnection>()); }
        }

        private T EnsureCurrent<T>(T feature) where T : class
        {
            if (_revision == _context.Revision) return feature;

            _request = null;
            _connection = null;
            _revision = _context.Revision;
            return null;
        } 

        public override HttpContextBase HttpContext { get { return _context; } }

        public override Uri Uri
        {
            get { return IHttpRequest.Uri; }
        }

        //public override Uri Uri { get { _request} }

        public override PathString PathBase
        {
            get { return new PathString(IHttpRequest.PathBase); }
            set { IHttpRequest.PathBase = value.Value; }
        }

        public override PathString Path
        {
            get { return new PathString(IHttpRequest.Path); }
            set { IHttpRequest.Path = value.Value; }
        }

        public override QueryString QueryString
        {
            get { return new QueryString(IHttpRequest.QueryString); }
            set { IHttpRequest.QueryString = value.Value; }
        }

        public override Stream Body
        {
            get { return IHttpRequest.Body; }
            set { IHttpRequest.Body = value; }
        }
    }
}