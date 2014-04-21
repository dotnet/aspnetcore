using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.FeatureModel;

namespace Microsoft.AspNet.Owin
{
    using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;

    public class OwinFeatureCollection :
        IFeatureCollection,
        IHttpRequestInformation,
        IHttpResponseInformation,
        IHttpConnection,
        IHttpSendFile,
        IHttpTransportLayerSecurity,
        ICanHasOwinEnvironment
    {
        public IDictionary<string, object> Environment { get; set; }

        public OwinFeatureCollection(IDictionary<string, object> environment)
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

        string IHttpRequestInformation.Protocol
        {
            get { return Prop<string>(OwinConstants.RequestProtocol); }
            set { Prop(OwinConstants.RequestProtocol, value); }
        }

        string IHttpRequestInformation.Scheme
        {
            get { return Prop<string>(OwinConstants.RequestScheme); }
            set { Prop(OwinConstants.RequestScheme, value); }
        }

        string IHttpRequestInformation.Method
        {
            get { return Prop<string>(OwinConstants.RequestMethod); }
            set { Prop(OwinConstants.RequestMethod, value); }
        }

        string IHttpRequestInformation.PathBase
        {
            get { return Prop<string>(OwinConstants.RequestPathBase); }
            set { Prop(OwinConstants.RequestPathBase, value); }
        }

        string IHttpRequestInformation.Path
        {
            get { return Prop<string>(OwinConstants.RequestPath); }
            set { Prop(OwinConstants.RequestPath, value); }
        }

        string IHttpRequestInformation.QueryString
        {
            get { return Prop<string>(OwinConstants.RequestQueryString); }
            set { Prop(OwinConstants.RequestQueryString, value); }
        }

        IDictionary<string, string[]> IHttpRequestInformation.Headers
        {
            get { return Prop<IDictionary<string, string[]>>(OwinConstants.RequestHeaders); }
            set { Prop(OwinConstants.RequestHeaders, value); }
        }

        Stream IHttpRequestInformation.Body
        {
            get { return Prop<Stream>(OwinConstants.RequestBody); }
            set { Prop(OwinConstants.RequestBody, value); }
        }

        int IHttpResponseInformation.StatusCode
        {
            get { return Prop<int>(OwinConstants.ResponseStatusCode); }
            set { Prop(OwinConstants.ResponseStatusCode, value); }
        }

        string IHttpResponseInformation.ReasonPhrase
        {
            get { return Prop<string>(OwinConstants.ResponseReasonPhrase); }
            set { Prop(OwinConstants.ResponseReasonPhrase, value); }
        }

        IDictionary<string, string[]> IHttpResponseInformation.Headers
        {
            get { return Prop<IDictionary<string, string[]>>(OwinConstants.ResponseHeaders); }
            set { Prop(OwinConstants.ResponseHeaders, value); }
        }

        Stream IHttpResponseInformation.Body
        {
            get { return Prop<Stream>(OwinConstants.ResponseBody); }
            set { Prop(OwinConstants.ResponseBody, value); }
        }

        void IHttpResponseInformation.OnSendingHeaders(Action<object> callback, object state)
        {
            var register = Prop<Action<Action<object>, object>>(OwinConstants.CommonKeys.OnSendingHeaders);
            if (register == null)
            {
                throw new NotSupportedException(OwinConstants.CommonKeys.OnSendingHeaders);
            }
            register(callback, state);
        }

        IPAddress IHttpConnection.RemoteIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.RemoteIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.RemoteIpAddress, value.ToString()); }
        }

        IPAddress IHttpConnection.LocalIpAddress
        {
            get { return IPAddress.Parse(Prop<string>(OwinConstants.CommonKeys.LocalIpAddress)); }
            set { Prop(OwinConstants.CommonKeys.LocalIpAddress, value.ToString()); }
        }

        int IHttpConnection.RemotePort
        {
            get { return int.Parse(Prop<string>(OwinConstants.CommonKeys.RemotePort)); }
            set { Prop(OwinConstants.CommonKeys.RemotePort, value.ToString(CultureInfo.InvariantCulture)); }
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

        private bool SupportsSendFile
        {
            get
            {
                object obj;
                return Environment.TryGetValue(OwinConstants.SendFiles.SendAsync, out obj) && obj != null;
            }
        }

        Task IHttpSendFile.SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
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
                if (string.Equals("https", ((IHttpRequestInformation)this).Scheme, StringComparison.OrdinalIgnoreCase)
                    && (Environment.TryGetValue(OwinConstants.CommonKeys.LoadClientCertAsync, out obj)
                        || Environment.TryGetValue(OwinConstants.CommonKeys.ClientCertificate, out obj))
                    && obj != null)
                {
                    return true;
                }
                return false;
            }
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
            if (key.GetTypeInfo().IsAssignableFrom(this.GetType().GetTypeInfo()))
            {
                // Check for conditional features
                if (key == typeof(IHttpSendFile))
                {
                    return SupportsSendFile;
                }
                else if (key == typeof(IHttpTransportLayerSecurity))
                {
                    return SupportsClientCerts;
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
                    typeof(IHttpRequestInformation),
                    typeof(IHttpResponseInformation),
                    typeof(IHttpConnection),
                    typeof(ICanHasOwinEnvironment),
                };
                if (SupportsSendFile)
                {
                    keys.Add(typeof(IHttpSendFile));
                }
                if (SupportsClientCerts)
                {
                    keys.Add(typeof(IHttpTransportLayerSecurity));
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

        public void CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
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

