using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class FeatureContext : IHttpRequestInformation, IHttpConnection, IHttpResponseInformation, IHttpSendFile, IHttpTransportLayerSecurity
    {
        private RequestContext _requestContext;
        private FeatureCollection _features;

        private Stream _requestBody;
        private IDictionary<string, string[]> _requestHeaders;
        private string _scheme;
        private string _httpMethod;
        private string _httpProtocolVersion;
        private string _query;
        private string _pathBase;
        private string _path;
        private IPAddress _remoteIpAddress;
        private IPAddress _localIpAddress;
        private int? _remotePort;
        private int? _localPort;
        private bool? _isLocal;
        private X509Certificate _clientCert;
        private Stream _responseStream;
        private IDictionary<string, string[]> _responseHeaders;

        internal FeatureContext(RequestContext requestContext)
        {
            _requestContext = requestContext;
            _features = new FeatureCollection();
            PopulateFeatures();
        }

        internal IFeatureCollection Features
        {
            get { return _features; }
        }

        private Request Request
        {
            get { return _requestContext.Request; }
        }

        private Response Response
        {
            get { return _requestContext.Response; }
        }

        private void PopulateFeatures()
        {
            _features.Add(typeof(IHttpRequestInformation), this);
            _features.Add(typeof(IHttpConnection), this);
            if (Request.IsSecureConnection)
            {
                // TODO: Should this feature be conditional? Should we add this for HTTP requests?
                _features.Add(typeof(IHttpTransportLayerSecurity), this);
            }
            _features.Add(typeof(IHttpResponseInformation), this);
            _features.Add(typeof(IHttpSendFile), this);

            // TODO: 
            // _environment.CallCancelled = _cts.Token;
            // _environment.User = _request.User;
            // Opaque/WebSockets
            // Channel binding

            /*
            // Server
            _environment.Listener = _server;
            _environment.ConnectionId = _request.ConnectionId;            
             */
        }

        #region IHttpRequestInformation

        Stream IHttpRequestInformation.Body
        {
            get
            {
                if (_requestBody == null)
                {
                    _requestBody = Request.Body;
                }
                return _requestBody;
            }
            set { _requestBody = value; }
        }

        IDictionary<string, string[]> IHttpRequestInformation.Headers
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = Request.Headers;
                }
                return _requestHeaders;
            }
            set { _requestHeaders = value; }
        }

        string IHttpRequestInformation.Method
        {
            get
            {
                if (_httpMethod == null)
                {
                    _httpMethod = Request.Method;
                }
                return _httpMethod;
            }
            set { _httpMethod = value; }
        }

        string IHttpRequestInformation.Path
        {
            get
            {
                if (_path == null)
                {
                    _path = Request.Path;
                }
                return _path;
            }
            set { _path = value; }
        }

        string IHttpRequestInformation.PathBase
        {
            get
            {
                if (_pathBase == null)
                {
                    _pathBase = Request.PathBase;
                }
                return _pathBase;
            }
            set { _pathBase = value; }
        }

        string IHttpRequestInformation.Protocol
        {
            get
            {
                if (_httpProtocolVersion == null)
                {
                    if (Request.ProtocolVersion.Major == 1)
                    {
                        if (Request.ProtocolVersion.Minor == 1)
                        {
                            _httpProtocolVersion = "HTTP/1.1";
                        }
                        else if (Request.ProtocolVersion.Minor == 0)
                        {
                            _httpProtocolVersion = "HTTP/1.0";
                        }
                    }

                    _httpProtocolVersion = "HTTP/" + Request.ProtocolVersion.ToString(2);
                }
                return _httpProtocolVersion;
            }
            set { _httpProtocolVersion = value; }
        }

        string IHttpRequestInformation.QueryString
        {
            get
            {
                if (_query == null)
                {
                    _query = Request.QueryString;
                }
                return _query;
            }
            set { _query = value; }
        }

        string IHttpRequestInformation.Scheme
        {
            get
            {
                if (_scheme == null)
                {
                    _scheme = Request.Scheme;
                }
                return _scheme;
            }
            set { _scheme = value; }
        }
        #endregion
        #region IHttpConnection
        bool IHttpConnection.IsLocal
        {
            get
            {
                if (_isLocal == null)
                {
                    _isLocal = Request.IsLocal;
                }
                return _isLocal.Value;
            }
            set { _isLocal = value; }
        }

        IPAddress IHttpConnection.LocalIpAddress
        {
            get
            {
                if (_localIpAddress == null)
                {
                    _localIpAddress = Request.LocalIpAddress;
                }
                return _localIpAddress;
            }
            set { _localIpAddress = value; }
        }

        IPAddress IHttpConnection.RemoteIpAddress
        {
            get
            {
                if (_remoteIpAddress == null)
                {
                    _remoteIpAddress = Request.RemoteIpAddress;
                }
                return _remoteIpAddress;
            }
            set { _remoteIpAddress = value; }
        }

        int IHttpConnection.LocalPort
        {
            get
            {
                if (_localPort == null)
                {
                    _localPort = Request.LocalPort;
                }
                return _localPort.Value;
            }
            set { _localPort = value; }
        }

        int IHttpConnection.RemotePort
        {
            get
            {
                if (_remotePort == null)
                {
                    _remotePort = Request.RemotePort;
                }
                return _remotePort.Value;
            }
            set { _remotePort = value; }
        }
        #endregion
        #region IHttpTransportLayerSecurity
        X509Certificate IHttpTransportLayerSecurity.ClientCertificate
        {
            get
            {
                if (_clientCert == null)
                {
                    _clientCert = Request.GetClientCertificateAsync().Result; // TODO: Sync;
                }
                return _clientCert;
            }
            set { _clientCert = value; }
        }

        async Task IHttpTransportLayerSecurity.LoadAsync()
        {
            if (_clientCert == null)
            {
                _clientCert = await Request.GetClientCertificateAsync();
            }
        }
        #endregion
        #region IHttpResponseInformation
        Stream IHttpResponseInformation.Body
        {
            get
            {
                if (_responseStream == null)
                {
                    _responseStream = Response.Body;
                }
                return _responseStream;
            }
            set { _responseStream = value; }
        }

        IDictionary<string, string[]> IHttpResponseInformation.Headers
        {
            get
            {
                if (_responseHeaders == null)
                {
                    _responseHeaders = Response.Headers;
                }
                return _responseHeaders;
            }
            set { _responseHeaders = value; }
        }

        void IHttpResponseInformation.OnSendingHeaders(Action<object> callback, object state)
        {
            Response.OnSendingHeaders(callback, state);
        }

        string IHttpResponseInformation.ReasonPhrase
        {
            get { return Response.ReasonPhrase; }
            set { Response.ReasonPhrase = value; }
        }

        int IHttpResponseInformation.StatusCode
        {
            get { return Response.StatusCode; }
            set { Response.StatusCode = value; }
        }
        #endregion
        Task IHttpSendFile.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            return Response.SendFileAsync(path, offset, length, cancellation);
        }
    }
}
