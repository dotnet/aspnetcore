using System;
using System.IO;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Interfaces;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpRequest : HttpRequest
    {
        private readonly DefaultHttpContext _context;
        private int _revision;
        private IHttpRequestInformation _request;
        private IHttpConnection _connection;

        public DefaultHttpRequest(DefaultHttpContext context)
        {
            _context = context;
        }

        private IHttpRequestInformation IHttpRequest
        {
            get { return EnsureCurrent(_request) ?? (_request = _context.GetInterface<IHttpRequestInformation>()); }
        }

        private IHttpConnection IHttpConnection
        {
            get { return EnsureCurrent(_connection) ?? (_connection = _context.GetInterface<IHttpConnection>()); }
        }

        private T EnsureCurrent<T>(T feature) where T : class
        {
            if (_revision == _context.Revision) return feature;

            _request = null;
            _connection = null;
            _revision = _context.Revision;
            return null;
        } 

        public override HttpContext HttpContext { get { return _context; } }

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

        public override string Method
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string Scheme
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsSecure
        {
            get { throw new NotImplementedException(); }
        }

        public override HostString Host
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IReadableStringCollection Query
        {
            get { throw new NotImplementedException(); }
        }

        public override string Protocol
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IHeaderDictionary Headers
        {
            get { throw new NotImplementedException(); }
        }

        public override IReadableStringCollection Cookies
        {
            get { throw new NotImplementedException(); }
        }

        public override System.Threading.CancellationToken CallCanceled
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}