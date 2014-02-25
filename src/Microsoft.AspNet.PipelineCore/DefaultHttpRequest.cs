using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore.Collections;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultHttpRequest : HttpRequest
    {
        private readonly DefaultHttpContext _context;
        private readonly IFeatureCollection _features;

        private FeatureReference<IHttpRequestInformation> _request = FeatureReference<IHttpRequestInformation>.Default;
        private FeatureReference<IHttpConnection> _connection = FeatureReference<IHttpConnection>.Default;
        private FeatureReference<IHttpTransportLayerSecurity> _transportLayerSecurity = FeatureReference<IHttpTransportLayerSecurity>.Default;
        private FeatureReference<ICanHasQuery> _canHasQuery = FeatureReference<ICanHasQuery>.Default;
        private FeatureReference<ICanHasForm> _canHasForm = FeatureReference<ICanHasForm>.Default;
        private FeatureReference<ICanHasRequestCookies> _canHasCookies = FeatureReference<ICanHasRequestCookies>.Default;

        public DefaultHttpRequest(DefaultHttpContext context, IFeatureCollection features)
        {
            _context = context;
            _features = features;
        }

        private IHttpRequestInformation HttpRequestInformation
        {
            get { return _request.Fetch(_features); }
        }

        private IHttpConnection HttpConnection
        {
            get { return _connection.Fetch(_features); }
        }

        private IHttpTransportLayerSecurity HttpTransportLayerSecurity
        {
            get { return _transportLayerSecurity.Fetch(_features); }
        }

        private ICanHasQuery CanHasQuery
        {
            get { return _canHasQuery.Fetch(_features) ?? _canHasQuery.Update(_features, new DefaultCanHasQuery(_features)); }
        }

        private ICanHasForm CanHasForm
        {
            get { return _canHasForm.Fetch(_features) ?? _canHasForm.Update(_features, new DefaultCanHasForm(_features)); }
        }

        private ICanHasRequestCookies CanHasRequestCookies
        {
            get { return _canHasCookies.Fetch(_features) ?? _canHasCookies.Update(_features, new DefaultCanHasRequestCookies(_features)); }
        }


        public override HttpContext HttpContext { get { return _context; } }

        public override PathString PathBase
        {
            get { return new PathString(HttpRequestInformation.PathBase); }
            set { HttpRequestInformation.PathBase = value.Value; }
        }

        public override PathString Path
        {
            get { return new PathString(HttpRequestInformation.Path); }
            set { HttpRequestInformation.Path = value.Value; }
        }

        public override QueryString QueryString
        {
            get { return new QueryString(HttpRequestInformation.QueryString); }
            set { HttpRequestInformation.QueryString = value.Value; }
        }

        public override Stream Body
        {
            get { return HttpRequestInformation.Body; }
            set { HttpRequestInformation.Body = value; }
        }

        public override string Method
        {
            get { return HttpRequestInformation.Method; }
            set { HttpRequestInformation.Method = value; }
        }

        public override string Scheme
        {
            get { return HttpRequestInformation.Scheme; }
            set { HttpRequestInformation.Scheme = value; }
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
            get { return CanHasQuery.Query; }
        }

        public override Task<IReadableStringCollection> GetFormAsync()
        {
            return CanHasForm.GetFormAsync();
        }

        public override string Protocol
        {
            get { return HttpRequestInformation.Protocol; }
            set { HttpRequestInformation.Protocol = value; }
        }

        public override IHeaderDictionary Headers
        {
            get { return new HeaderDictionary(HttpRequestInformation.Headers); }
        }

        public override IReadableStringCollection Cookies
        {
            get { return CanHasRequestCookies.Cookies; }
        }

        public override System.Threading.CancellationToken CallCanceled
        {
            get
            {
                // TODO: Which feature exposes this?
                return CancellationToken.None;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}