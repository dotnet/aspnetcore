// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Owin
{
    using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;

    public class OwinFeatureCollection :
        IFeatureCollection,
        IHttpRequestFeature,
        IHttpResponseFeature,
        IHttpResponseBodyFeature,
        IHttpConnectionFeature,
        ITlsConnectionFeature,
        IHttpRequestIdentifierFeature,
        IHttpRequestLifetimeFeature,
        IHttpAuthenticationFeature,
        IHttpWebSocketFeature,
        IOwinEnvironmentFeature
    {
        public IDictionary<string, object> Environment { get; set; }
        private PipeWriter _responseBodyWrapper;
        private bool _headersSent;

        public OwinFeatureCollection(IDictionary<string, object> environment)
        {
            Environment = environment;
            SupportsWebSockets = true;

            var register = Prop<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            register?.Invoke(state =>
            {
                var collection = (OwinFeatureCollection)state;
                collection._headersSent = true;
            }, this);
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

        string IHttpRequestFeature.RawTarget
        {
            get { return string.Empty; }
            set { throw new NotSupportedException(); }
        }

        IHeaderDictionary IHttpRequestFeature.Headers
        {
            get { return Utilities.MakeHeaderDictionary(Prop<IDictionary<string, string[]>>(OwinConstants.RequestHeaders)); }
            set { Prop(OwinConstants.RequestHeaders, Utilities.MakeDictionaryStringArray(value)); }
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get { return Prop<string>(OwinConstants.RequestId); }
            set { Prop(OwinConstants.RequestId, value); }
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

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get { return Utilities.MakeHeaderDictionary(Prop<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders)); }
            set { Prop(OwinConstants.ResponseHeaders, Utilities.MakeDictionaryStringArray(value)); }
        }

        Stream IHttpResponseFeature.Body
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
            set { Prop(OwinConstants.ResponseBody, value); }
        }

        Stream IHttpResponseBodyFeature.Stream
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
        }

        PipeWriter IHttpResponseBodyFeature.Writer
        {
            get
            {
                if (_responseBodyWrapper == null)
                {
                    _responseBodyWrapper = PipeWriter.Create(Prop<Stream>(OwinConstants.ResponseBody), new StreamPipeWriterOptions(leaveOpen: true));
                }

                return _responseBodyWrapper;
            }
        }

        bool IHttpResponseFeature.HasStarted
        {
            get { return _headersSent; }
        }

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            var register = Prop<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            if (register == null)
            {
                throw new NotSupportedException(OwinConstants.CommonKeys.OnSendingHeaders);
            }

            // Need to block on the callback since we can't change the OWIN signature to be async
            register(s => callback(s).GetAwaiter().GetResult(), state);
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotSupportedException();
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

        string IHttpConnectionFeature.ConnectionId
        {
            get { return Prop<string>(OwinConstants.CommonKeys.ConnectionId); }
            set { Prop(OwinConstants.CommonKeys.ConnectionId, value); }
        }

        Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
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

        X509Certificate2 ITlsConnectionFeature.ClientCertificate
        {
            get { return Prop<X509Certificate2>(OwinConstants.CommonKeys.ClientCertificate); }
            set { Prop(OwinConstants.CommonKeys.ClientCertificate, value); }
        }

        async Task<X509Certificate2> ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            var loadAsync = Prop<Func<Task>>(OwinConstants.CommonKeys.LoadClientCertAsync);
            if (loadAsync != null)
            {
                await loadAsync();
            }
            return Prop<X509Certificate2>(OwinConstants.CommonKeys.ClientCertificate);
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get { return Prop<CancellationToken>(OwinConstants.CallCancelled); }
            set { Prop(OwinConstants.CallCancelled, value); }
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            throw new NotImplementedException();
        }

        ClaimsPrincipal IHttpAuthenticationFeature.User
        {
            get
            {
                return Prop<ClaimsPrincipal>(OwinConstants.RequestUser)
                    ?? Utilities.MakeClaimsPrincipal(Prop<IPrincipal>(OwinConstants.Security.User));
            }
            set
            {
                Prop(OwinConstants.RequestUser, value);
                Prop(OwinConstants.Security.User, value);
            }
        }

        /// <summary>
        /// Gets or sets if the underlying server supports WebSockets. This is enabled by default.
        /// The value should be consistent across requests.
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

        Task<WebSocket> IHttpWebSocketFeature.AcceptAsync(WebSocketAcceptContext context)
        {
            object obj;
            if (!Environment.TryGetValue(OwinConstants.WebSocket.AcceptAlt, out obj))
            {
                throw new NotSupportedException("WebSockets are not supported"); // TODO: LOC
            }
            var accept = (Func<WebSocketAcceptContext, Task<WebSocket>>)obj;
            return accept(context);
        }

        // IFeatureCollection

        public int Revision
        {
            get { return 0; } // Not modifiable
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public object this[Type key]
        {
            get { return Get(key); }
            set { throw new NotSupportedException(); }
        }

        private bool SupportsInterface(Type key)
        {
            // Does this type implement the requested interface?
            if (key.GetTypeInfo().IsAssignableFrom(GetType().GetTypeInfo()))
            {
                // Check for conditional features
                if (key == typeof(ITlsConnectionFeature))
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

        public object Get(Type key)
        {
            if (SupportsInterface(key))
            {
                return this;
            }
            return null;
        }

        public void Set(Type key, object value)
        {
            throw new NotSupportedException();
        }

        public TFeature Get<TFeature>()
        {
            return (TFeature)this[typeof(TFeature)];
        }

        public void Set<TFeature>(TFeature instance)
        {
            this[typeof(TFeature)] = instance;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
        {
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpResponseFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpResponseBodyFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpConnectionFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestIdentifierFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpRequestLifetimeFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IHttpAuthenticationFeature), this);
            yield return new KeyValuePair<Type, object>(typeof(IOwinEnvironmentFeature), this);

            // Check for conditional features
            if (SupportsClientCerts)
            {
                yield return new KeyValuePair<Type, object>(typeof(ITlsConnectionFeature), this);
            }
            if (SupportsWebSockets)
            {
                yield return new KeyValuePair<Type, object>(typeof(IHttpWebSocketFeature), this);
            }
        }

        void IHttpResponseBodyFeature.DisableBuffering()
        {
        }

        async Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellationToken)
        {
            if (_responseBodyWrapper != null)
            {
                await _responseBodyWrapper.FlushAsync(cancellationToken);
            }

            // The pipe may or may not have flushed the stream. Make sure the stream gets flushed to trigger response start.
            await Prop<Stream>(OwinConstants.ResponseBody).FlushAsync(cancellationToken);
        }

        Task IHttpResponseBodyFeature.CompleteAsync()
        {
            if (_responseBodyWrapper != null)
            {
                return _responseBodyWrapper.FlushAsync().AsTask();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}

