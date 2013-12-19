using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpEnvironment;
using Microsoft.AspNet.Interfaces;

namespace Microsoft.AspNet.PipelineCore.Owin
{
    public class OwinHttpEnvironment :
        IHttpRequest, IHttpResponse, IHttpConnection, IHttpSendFile, IHttpTransportLayerSecurity
    {
        public IDictionary<string, object> Environment { get; set; }

        public OwinHttpEnvironment(IDictionary<string, object> environment)
        {
            Environment = environment;
        }

        T Prop<T>(string key)
        {
            object value;
            if (Environment.TryGetValue(key, out value) && value is T)
            {
                return (T)value;
            }
            return default(T);
        }

        void Prop(string key, object value)
        {
            Environment[key] = value;
        }

        string IHttpRequest.Protocol
        {
            get { return Prop<string>("owin.RequestProtocol"); }
            set { Prop("owin.RequestProtocol", value); }
        }

        Uri IHttpRequest.Uri
        {
            get { throw new NotImplementedException(); }
        }

        string IHttpRequest.Scheme
        {
            get { return Prop<string>("owin.RequestScheme"); }
            set { Prop("owin.RequestScheme", value); }
        }

        string IHttpRequest.Method
        {
            get { return Prop<string>("owin.RequestMethod"); }
            set { Prop("owin.RequestMethod", value); }
        }

        string IHttpRequest.PathBase
        {
            get { return Prop<string>("owin.RequestPathBase"); }
            set { Prop("owin.RequestPathBase", value); }
        }

        string IHttpRequest.Path
        {
            get { return Prop<string>("owin.RequestPath"); }
            set { Prop("owin.RequestPath", value); }
        }

        string IHttpRequest.QueryString
        {
            get { return Prop<string>("owin.RequestQueryString"); }
            set { Prop("owin.RequestQueryString", value); }
        }

        IDictionary<string, string[]> IHttpRequest.Headers
        {
            get { return Prop<IDictionary<string, string[]>>("owin.RequestHeaders"); }
            set { Prop("owin.RequestHeaders", value); }
        }

        Stream IHttpRequest.Body
        {
            get { return Prop<Stream>("owin.RequestBody"); }
            set { Prop("owin.RequestBody", value); }
        }

        int IHttpResponse.StatusCode
        {
            get { return Prop<int>("owin.ResponseStatusCode"); }
            set { Prop("owin.ResponseStatusCode", value); }
        }

        string IHttpResponse.ReasonPhrase
        {
            get { return Prop<string>("owin.ResponseReasonPhrase"); }
            set { Prop("owin.ResponseReasonPhrase", value); }
        }

        IDictionary<string, string[]> IHttpResponse.Headers
        {
            get { return Prop<IDictionary<string, string[]>>("owin.ResponseHeaders"); }
            set { Prop("owin.ResponseHeaders", value); }
        }

        Stream IHttpResponse.Body
        {
            get { return Prop<Stream>("owin.ResponseBody"); }
            set { Prop("owin.ResponseBody", value); }
        }

        void IHttpResponse.OnSendingHeaders(Action<object> callback, object state)
        {
            // TODO: 
        }

        IPAddress IHttpConnection.RemoteIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.RemoteIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.RemoteIpAddress, value.ToString()); }
        }

        int IHttpConnection.RemotePort
        {
            get { return int.Parse(Prop<string>(OwinConstants.CommonKeys.RemotePort)); }
            set { Prop(OwinConstants.CommonKeys.RemotePort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        IPAddress IHttpConnection.LocalIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.LocalIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.LocalIpAddress, value.ToString()); }
        }

        int IHttpConnection.LocalPort
        {
            get { return int.Parse(Prop<string>(OwinConstants.CommonKeys.LocalPort)); }
            set { Prop(OwinConstants.CommonKeys.LocalPort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        bool IHttpConnection.IsLocal
        {
            get { return Prop<bool>(OwinConstants.CommonKeys.IsLocal); }
            set { Prop(OwinConstants.CommonKeys.LocalPort, value); }
        }

        Task IHttpSendFile.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            throw new NotImplementedException();
        }

        X509Certificate IHttpTransportLayerSecurity.ClientCertificate
        {
            get { return Prop<X509Certificate>(OwinConstants.CommonKeys.ClientCertificate); }
            set { Prop(OwinConstants.CommonKeys.ClientCertificate, value); }
        }

        Task IHttpTransportLayerSecurity.LoadAsync()
        {
            throw new NotImplementedException();
        }
    }
}