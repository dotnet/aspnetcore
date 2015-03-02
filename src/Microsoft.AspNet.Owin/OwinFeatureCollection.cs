// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.AspNet.Http.Interfaces.Authentication;

namespace Microsoft.AspNet.Owin
{
    using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;

    public class OwinFeatureCollection :
        IFeatureCollection,
        IHttpRequestFeature,
        IHttpResponseFeature,
        IHttpConnectionFeature,
        IHttpSendFileFeature,
        IHttpClientCertificateFeature,
        IHttpRequestLifetimeFeature,
        IHttpAuthenticationFeature,
        IHttpWebSocketFeature,
        IOwinEnvironmentFeature
    {
        public IDictionary<string, object> Environment { get; set; }
        private bool _headersSent;

        public OwinFeatureCollection(IDictionary<string, object> environment)
        {
            Environment = environment;
            SupportsWebSockets = true;

            var register = Prop<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            if (register != null)
            {
                register(state =>
                {
                    var collection = (OwinFeatureCollection)state;
                    collection._headersSent = true;
                }, this);
            }
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

        string IHttpRequestFeature.Protocol
        {
            get { return Prop<string>(OwinConstants.RequestProtocol); }
            set { Prop(OwinConstants.RequestProtocol, value); }
        }

        string IHttpRequestFeature.Scheme
        {
            get { return Prop<string>(OwinConstants.RequestScheme); }
            set { Prop(OwinConstants.RequestScheme, value); }
        }

        string IHttpRequestFeature.Method
        {
            get { return Prop<string>(OwinConstants.RequestMethod); }
            set { Prop(OwinConstants.RequestMethod, value); }
        }

        string IHttpRequestFeature.PathBase
        {
            get { return Prop<string>(OwinConstants.RequestPathBase); }
            set { Prop(OwinConstants.RequestPathBase, value); }
        }

        string IHttpRequestFeature.Path
        {
            get { return Prop<string>(OwinConstants.RequestPath); }
            set { Prop(OwinConstants.RequestPath, value); }
        }

        string IHttpRequestFeature.QueryString
        {
            get { return Utilities.AddQuestionMark(Prop<string>(OwinConstants.RequestQueryString)); }
            set { Prop(OwinConstants.RequestQueryString, Utilities.RemoveQuestionMark(value)); }
        }

        IDictionary<string, string[]> IHttpRequestFeature.Headers
        {
            get { return Prop<IDictionary<string, string[]>>(OwinConstants.RequestHeaders); }
            set { Prop(OwinConstants.RequestHeaders, value); }
        }

        Stream IHttpRequestFeature.Body
        {
            get { return Prop<Stream>(OwinConstants.RequestBody); }
            set { Prop(OwinConstants.RequestBody, value); }
        }

        int IHttpResponseFeature.StatusCode
        {
            get { return Prop<int>(OwinConstants.ResponseStatusCode); }
            set { Prop(OwinConstants.ResponseStatusCode, value); }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get { return Prop<string>(OwinConstants.ResponseReasonPhrase); }
            set { Prop(OwinConstants.ResponseReasonPhrase, value); }
        }

        IDictionary<string, string[]> IHttpResponseFeature.Headers
        {
            get { return Prop<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders); }
            set { Prop(OwinConstants.ResponseHeaders, value); }
        }

        Stream IHttpResponseFeature.Body
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
            set { Prop(OwinConstants.ResponseBody, value); }
        }

        bool IHttpResponseFeature.HeadersSent
        {
            get { return _headersSent; }
        }

        void IHttpResponseFeature.OnSendingHeaders(Action<object> callback, object state)
        {
            var register = Prop<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            if (register == null)
            {
                throw new NotSupportedException(OwinConstants.CommonKeys.OnSendingHeaders);
            }
            register(callback, state);
        }

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.RemoteIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.RemoteIpAddress, value.ToString()); }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.LocalIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.LocalIpAddress, value.ToString()); }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get { return int.Parse(Prop<string>(OwinConstants.CommonKeys.RemotePort)); }
            set { Prop(OwinConstants.CommonKeys.RemotePort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get { return int.Parse(Prop<string>(OwinConstants.CommonKeys.LocalPort)); }
            set { Prop(OwinConstants.CommonKeys.LocalPort, value.ToString(CultureInfo.InvariantCulture)); }
        }

        bool IHttpConnectionFeature.IsLocal
        {
            get { return Prop<bool>(OwinConstants.CommonKeys.IsLocal); }
            set { Prop(OwinConstants.CommonKeys.LocalPort, value); }
        }

        private bool SupportsSendFile
        {
            get
            {
                object obj;
                return Environment.TryGetValue(OwinConstants.SendFiles.SendAsync, out obj) && obj != null;
            }
        }

        Task IHttpSendFileFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
        {
            object obj;
            if (Environment.TryGetValue(OwinConstants.SendFiles.SendAsync, out obj))
            {
                var func = (SendFileFunc)obj;
                return func(path, offset, length, cancellation);
            }
            throw new NotSupportedException(OwinConstants.SendFiles.SendAsync);
        }

        private bool SupportsClientCerts
        {
            get
            {
                object obj;
                if (string.Equals("https", ((IHttpRequestFeature)this).Scheme, StringComparison.OrdinalIgnoreCase)
                    && (Environment.TryGetValue(OwinConstants.CommonKeys.LoadClientCertAsync, out obj)
                        || Environment.TryGetValue(OwinConstants.CommonKeys.ClientCertificate, out obj))
                    && obj != null)
                {
                    return true;
                }
                return false;
            }
        }

        X509Certificate IHttpClientCertificateFeature.ClientCertificate
        {
            get { return Prop<X509Certificate>(OwinConstants.CommonKeys.ClientCertificate); }
            set { Prop(OwinConstants.CommonKeys.ClientCertificate, value); }
        }

        async Task<X509Certificate> IHttpClientCertificateFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            var loadAsync = Prop<Func<Task>>(OwinConstants.CommonKeys.LoadClientCertAsync);
            if (loadAsync != null)
            {
                await loadAsync();
            }
            return Prop<X509Certificate>(OwinConstants.CommonKeys.ClientCertificate);
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get { return Prop<CancellationToken>(OwinConstants.CallCancelled); }
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            throw new NotImplementedException();
        }

        ClaimsPrincipal IHttpAuthenticationFeature.User
        {
            get { return Utilities.MakeClaimsPrincipal(Prop<IPrincipal>(OwinConstants.Security.User)); }
            set { Prop(OwinConstants.Security.User, value); }
        }

        IAuthenticationHandler IHttpAuthenticationFeature.Handler { get; set; }

        /// <summary>
        /// Gets or sets if the underlying server supports WebSockets. This is enabled by default.
        /// The value should be consistant across requests.
        /// </summary>
        public bool SupportsWebSockets { get; set; }

        bool IHttpWebSocketFeature.IsWebSocketRequest
        {
            get
            {
                object obj;
                return Environment.TryGetValue(OwinConstants.WebSocket.AcceptAlt, out obj);
            }
        }

        Task<WebSocket> IHttpWebSocketFeature.AcceptAsync(IWebSocketAcceptContext context)
        {
            object obj;
            if (!Environment.TryGetValue(OwinConstants.WebSocket.AcceptAlt, out obj))
            {
                throw new NotSupportedException("WebSockets are not supported"); // TODO: LOC
            }
            var accept = (Func<IWebSocketAcceptContext, Task<WebSocket>>)obj;
            return accept(context);
        }

        public int Revision
        {
            get { return 0; } // Not modifiable
        }

        public void Add(Type key, object value)
        {
            throw new NotSupportedException();
        }

        public bool ContainsKey(Type key)
        {
            // Does this type implement the requested interface?
            if (key.GetTypeInfo().IsAssignableFrom(GetType().GetTypeInfo()))
            {
                // Check for conditional features
                if (key == typeof(IHttpSendFileFeature))
                {
                    return SupportsSendFile;
                }
                else if (key == typeof(IHttpClientCertificateFeature))
                {
                    return SupportsClientCerts;
                }
                else if (key == typeof(IHttpWebSocketFeature))
                {
                    return SupportsWebSockets;
                }

                // The rest of the features are always supported.
                return true;
            }
            return false;
        }

        public ICollection<Type> Keys
        {
            get
            {
                var keys = new List<Type>()
                {
                    typeof(IHttpRequestFeature),
                    typeof(IHttpResponseFeature),
                    typeof(IHttpConnectionFeature),
                    typeof(IOwinEnvironmentFeature),
                    typeof(IHttpRequestLifetimeFeature),
                    typeof(IHttpAuthenticationFeature),
                };
                if (SupportsSendFile)
                {
                    keys.Add(typeof(IHttpSendFileFeature));
                }
                if (SupportsClientCerts)
                {
                    keys.Add(typeof(IHttpClientCertificateFeature));
                }
                if (SupportsWebSockets)
                {
                    keys.Add(typeof(IHttpWebSocketFeature));
                }
                return keys;
            }
        }

        public bool Remove(Type key)
        {
            throw new NotSupportedException();
        }

        public bool TryGetValue(Type key, out object value)
        {
            if (ContainsKey(key))
            {
                value = this;
                return true;
            }
            value = null;
            return false;
        }

        public ICollection<object> Values
        {
            get { throw new NotSupportedException(); }
        }

        public object this[Type key]
        {
            get
            {
                object value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                throw new KeyNotFoundException(key.FullName);
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public void Add(KeyValuePair<Type, object> item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(KeyValuePair<Type, object> item)
        {
            object result;
            return TryGetValue(item.Key, out result) && result.Equals(item.Value);
        }

        public void CopyTo([NotNull] KeyValuePair<Type, object>[] array, int arrayIndex)
        {
            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, string.Empty);
            }
            var keys = Keys;
            if (keys.Count > array.Length - arrayIndex)
            {
                throw new ArgumentException();
            }

            foreach (var key in keys)
            {
                array[arrayIndex++] = new KeyValuePair<Type, object>(key, this[key]);
            }
        }

        public int Count
        {
            get { return Keys.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<Type, object> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            return Keys.Select(type => new KeyValuePair<Type, object>(type, this[type])).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
        }
    }
}

