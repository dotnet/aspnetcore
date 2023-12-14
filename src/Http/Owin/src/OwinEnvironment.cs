// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace Microsoft.AspNetCore.Owin;

using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;
using WebSocketAcceptAlt =
    Func
    <
        WebSocketAcceptContext, // WebSocket Accept parameters
        Task<WebSocket>
    >;

/// <summary>
/// A loosely-typed OWIN environment wrapper over an <see cref="HttpContext"/>.
/// </summary>
public class OwinEnvironment : IDictionary<string, object>
{
    private readonly HttpContext _context;
    private readonly IDictionary<string, FeatureMap> _entries;

    /// <summary>
    /// Initializes a new instance of <see cref="OwinEnvironment"/>.
    /// </summary>
    /// <param name="context">The request context.</param>
    public OwinEnvironment(HttpContext context)
    {
        if (context.Features.Get<IHttpRequestFeature>() == null)
        {
            throw new ArgumentException("Missing required feature: " + nameof(IHttpRequestFeature) + ".", nameof(context));
        }
        if (context.Features.Get<IHttpResponseFeature>() == null)
        {
            throw new ArgumentException("Missing required feature: " + nameof(IHttpResponseFeature) + ".", nameof(context));
        }

        _context = context;
        _entries = new Dictionary<string, FeatureMap>()
            {
                { OwinConstants.RequestProtocol, new FeatureMap<IHttpRequestFeature>(feature => feature.Protocol, () => string.Empty, (feature, value) => feature.Protocol = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.RequestScheme, new FeatureMap<IHttpRequestFeature>(feature => feature.Scheme, () => string.Empty, (feature, value) => feature.Scheme = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.RequestMethod, new FeatureMap<IHttpRequestFeature>(feature => feature.Method, () => string.Empty, (feature, value) => feature.Method = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.RequestPathBase, new FeatureMap<IHttpRequestFeature>(feature => feature.PathBase, () => string.Empty, (feature, value) => feature.PathBase = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.RequestPath, new FeatureMap<IHttpRequestFeature>(feature => feature.Path, () => string.Empty, (feature, value) => feature.Path = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.RequestQueryString, new FeatureMap<IHttpRequestFeature>(feature => Utilities.RemoveQuestionMark(feature.QueryString), () => string.Empty,
                    (feature, value) => feature.QueryString = Utilities.AddQuestionMark(Convert.ToString(value, CultureInfo.InvariantCulture))) },
                { OwinConstants.RequestHeaders, new FeatureMap<IHttpRequestFeature>(feature => Utilities.MakeDictionaryStringArray(feature.Headers), (feature, value) => feature.Headers = Utilities.MakeHeaderDictionary((IDictionary<string, string[]>)value)) },
                { OwinConstants.RequestBody, new FeatureMap<IHttpRequestFeature>(feature => feature.Body, () => Stream.Null, (feature, value) => feature.Body = (Stream)value) },
                { OwinConstants.RequestUser, new FeatureMap<IHttpAuthenticationFeature>(feature => feature.User, () => null, (feature, value) => feature.User = (ClaimsPrincipal)value) },

                { OwinConstants.ResponseStatusCode, new FeatureMap<IHttpResponseFeature>(feature => feature.StatusCode, () => 200, (feature, value) => feature.StatusCode = Convert.ToInt32(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.ResponseReasonPhrase, new FeatureMap<IHttpResponseFeature>(feature => feature.ReasonPhrase, (feature, value) => feature.ReasonPhrase = Convert.ToString(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.ResponseHeaders, new FeatureMap<IHttpResponseFeature>(feature => Utilities.MakeDictionaryStringArray(feature.Headers), (feature, value) => feature.Headers = Utilities.MakeHeaderDictionary((IDictionary<string, string[]>)value)) },
                { OwinConstants.ResponseBody, new FeatureMap<IHttpResponseBodyFeature>(feature => feature.Stream, () => Stream.Null, (feature, value) => context.Response.Body = (Stream)value) }, // DefaultHttpResponse.Body.Set has built in logic to handle replacing the feature.
                { OwinConstants.CommonKeys.OnSendingHeaders, new FeatureMap<IHttpResponseFeature>(
                    feature => new Action<Action<object>, object>((cb, state) => {
                        feature.OnStarting(s =>
                        {
                            cb(s);
                            return Task.CompletedTask;
                        }, state);
                    }))
                },

                { OwinConstants.CommonKeys.ConnectionId, new FeatureMap<IHttpConnectionFeature>(feature => feature.ConnectionId,
                    (feature, value) => feature.ConnectionId = Convert.ToString(value, CultureInfo.InvariantCulture)) },

                { OwinConstants.CommonKeys.LocalPort, new FeatureMap<IHttpConnectionFeature>(feature => feature.LocalPort.ToString(CultureInfo.InvariantCulture),
                    (feature, value) => feature.LocalPort = Convert.ToInt32(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.CommonKeys.RemotePort, new FeatureMap<IHttpConnectionFeature>(feature => feature.RemotePort.ToString(CultureInfo.InvariantCulture),
                    (feature, value) => feature.RemotePort = Convert.ToInt32(value, CultureInfo.InvariantCulture)) },

                { OwinConstants.CommonKeys.LocalIpAddress, new FeatureMap<IHttpConnectionFeature>(feature => feature.LocalIpAddress.ToString(),
                    (feature, value) => feature.LocalIpAddress = IPAddress.Parse(Convert.ToString(value, CultureInfo.InvariantCulture))) },
                { OwinConstants.CommonKeys.RemoteIpAddress, new FeatureMap<IHttpConnectionFeature>(feature => feature.RemoteIpAddress.ToString(),
                    (feature, value) => feature.RemoteIpAddress = IPAddress.Parse(Convert.ToString(value, CultureInfo.InvariantCulture))) },

                { OwinConstants.SendFiles.SendAsync, new FeatureMap<IHttpResponseBodyFeature>(feature => new SendFileFunc(feature.SendFileAsync)) },

                { OwinConstants.Security.User, new FeatureMap<IHttpAuthenticationFeature>(feature => feature.User,
                    ()=> null, (feature, value) => feature.User = Utilities.MakeClaimsPrincipal((IPrincipal)value),
                    () => new HttpAuthenticationFeature())
                },

                { OwinConstants.RequestId, new FeatureMap<IHttpRequestIdentifierFeature>(feature => feature.TraceIdentifier,
                    ()=> null, (feature, value) => feature.TraceIdentifier = (string)value,
                    () => new HttpRequestIdentifierFeature())
                }
            };

        // owin.CallCancelled is required but the feature may not be present.
        if (context.Features.Get<IHttpRequestLifetimeFeature>() != null)
        {
            _entries[OwinConstants.CallCancelled] = new FeatureMap<IHttpRequestLifetimeFeature>(feature => feature.RequestAborted);
        }
        else if (!_context.Items.ContainsKey(OwinConstants.CallCancelled))
        {
            _context.Items[OwinConstants.CallCancelled] = CancellationToken.None;
        }

        // owin.Version is required.
        if (!context.Items.ContainsKey(OwinConstants.OwinVersion))
        {
            _context.Items[OwinConstants.OwinVersion] = "1.0";
        }

        if (context.Request.IsHttps)
        {
            _entries.Add(OwinConstants.CommonKeys.ClientCertificate, new FeatureMap<ITlsConnectionFeature>(feature => feature.ClientCertificate,
                (feature, value) => feature.ClientCertificate = (X509Certificate2)value));
            _entries.Add(OwinConstants.CommonKeys.LoadClientCertAsync, new FeatureMap<ITlsConnectionFeature>(
                feature => new Func<Task>(() => feature.GetClientCertificateAsync(CancellationToken.None))));
        }

        if (context.WebSockets.IsWebSocketRequest)
        {
            _entries.Add(OwinConstants.WebSocket.AcceptAlt, new FeatureMap<IHttpWebSocketFeature>(feature => new WebSocketAcceptAlt(feature.AcceptAsync)));
        }

        _context.Items[typeof(HttpContext).FullName] = _context; // Store for lookup when we transition back out of OWIN
    }

    // Public in case there's a new/custom feature interface that needs to be added.
    /// <summary>
    /// Get the environment's feature maps.
    /// </summary>
    public IDictionary<string, FeatureMap> FeatureMaps
    {
        get { return _entries; }
    }

    void IDictionary<string, object>.Add(string key, object value)
    {
        if (_entries.ContainsKey(key))
        {
            throw new InvalidOperationException("Key already present");
        }
        _context.Items.Add(key, value);
    }

    bool IDictionary<string, object>.ContainsKey(string key)
    {
        return ((IDictionary<string, object>)this).TryGetValue(key, out _);
    }

    ICollection<string> IDictionary<string, object>.Keys
    {
        get
        {
            return _entries.Where(pair => pair.Value.TryGet(_context, out _))
                .Select(pair => pair.Key).Concat(_context.Items.Keys.Select(key => Convert.ToString(key, CultureInfo.InvariantCulture))).ToList();
        }
    }

    bool IDictionary<string, object>.Remove(string key)
    {
        if (_entries.Remove(key))
        {
            return true;
        }
        return _context.Items.Remove(key);
    }

    bool IDictionary<string, object>.TryGetValue(string key, out object value)
    {
        FeatureMap entry;
        if (_entries.TryGetValue(key, out entry) && entry.TryGet(_context, out value))
        {
            return true;
        }
        return _context.Items.TryGetValue(key, out value);
    }

    ICollection<object> IDictionary<string, object>.Values
    {
        get { throw new NotImplementedException(); }
    }

    object IDictionary<string, object>.this[string key]
    {
        get
        {
            FeatureMap entry;
            object value;
            if (_entries.TryGetValue(key, out entry) && entry.TryGet(_context, out value))
            {
                return value;
            }
            if (_context.Items.TryGetValue(key, out value))
            {
                return value;
            }
            throw new KeyNotFoundException(key);
        }
        set
        {
            FeatureMap entry;
            if (_entries.TryGetValue(key, out entry))
            {
                if (entry.CanSet)
                {
                    entry.Set(_context, value);
                }
                else
                {
                    _entries.Remove(key);
                    if (value != null)
                    {
                        _context.Items[key] = value;
                    }
                }
            }
            else
            {
                if (value == null)
                {
                    _context.Items.Remove(key);
                }
                else
                {
                    _context.Items[key] = value;
                }
            }
        }
    }

    void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
    {
        throw new NotImplementedException();
    }

    void ICollection<KeyValuePair<string, object>>.Clear()
    {
        _entries.Clear();
        _context.Items.Clear();
    }

    bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
    {
        throw new NotImplementedException();
    }

    void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex + _entries.Count + _context.Items.Count > array.Length)
        {
            throw new ArgumentException("Not enough available space in array", nameof(array));
        }

        // Causes an allocation of the enumerator/iterator but is easier to maintain
        foreach (var entryPair in this)
        {
            array[arrayIndex++] = entryPair;
        }
    }

    int ICollection<KeyValuePair<string, object>>.Count
    {
        get { return _entries.Count + _context.Items.Count; }
    }

    bool ICollection<KeyValuePair<string, object>>.IsReadOnly
    {
        get { return false; }
    }

    bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var entryPair in _entries)
        {
            object value;
            if (entryPair.Value.TryGet(_context, out value))
            {
                yield return new KeyValuePair<string, object>(entryPair.Key, value);
            }
        }
        foreach (var entryPair in _context.Items)
        {
            yield return new KeyValuePair<string, object>(Convert.ToString(entryPair.Key, CultureInfo.InvariantCulture), entryPair.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Maps OWIN keys to ASP.NET Core features.
    /// </summary>
    public class FeatureMap
    {
        /// <summary>
        /// Create a <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="featureInterface">The feature interface type.</param>
        /// <param name="getter">Value getter.</param>
        public FeatureMap(Type featureInterface, Func<object, object> getter)
            : this(featureInterface, getter, defaultFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="featureInterface">The feature interface type.</param>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory)
            : this(featureInterface, getter, defaultFactory, setter: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="featureInterface">The feature interface type.</param>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        public FeatureMap(Type featureInterface, Func<object, object> getter, Action<object, object> setter)
            : this(featureInterface, getter, defaultFactory: null, setter: setter)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="featureInterface">The feature interface type.</param>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory, Action<object, object> setter)
            : this(featureInterface, getter, defaultFactory, setter, featureFactory: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="featureInterface">The feature interface type.</param>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        /// <param name="featureFactory">Feature factory delegate.</param>
        public FeatureMap(Type featureInterface, Func<object, object> getter, Func<object> defaultFactory, Action<object, object> setter, Func<object> featureFactory)
        {
            FeatureInterface = featureInterface;
            Getter = getter;
            Setter = setter;
            DefaultFactory = defaultFactory;
            FeatureFactory = featureFactory;
        }

        private Type FeatureInterface { get; set; }
        private Func<object, object> Getter { get; set; }
        private Action<object, object> Setter { get; set; }
        private Func<object> DefaultFactory { get; set; }
        private Func<object> FeatureFactory { get; set; }

        /// <summary>
        /// Gets a value indicating whether the feature map is settable.
        /// </summary>
        public bool CanSet
        {
            get { return Setter != null; }
        }

        internal bool TryGet(HttpContext context, out object value)
        {
            object featureInstance = context.Features[FeatureInterface];
            if (featureInstance == null)
            {
                value = null;
                return false;
            }
            value = Getter(featureInstance);
            if (value == null && DefaultFactory != null)
            {
                value = DefaultFactory();
            }
            return true;
        }

        internal void Set(HttpContext context, object value)
        {
            var feature = context.Features[FeatureInterface];
            if (feature == null)
            {
                if (FeatureFactory == null)
                {
                    throw new InvalidOperationException("Missing feature: " + FeatureInterface.FullName); // TODO: LOC
                }
                else
                {
                    feature = FeatureFactory();
                    context.Features[FeatureInterface] = feature;
                }
            }
            Setter(feature, value);
        }
    }

    /// <summary>
    /// Maps OWIN keys to ASP.NET Core features.
    /// </summary>
    /// <typeparam name="TFeature">Feature interface type.</typeparam>
    public class FeatureMap<TFeature> : FeatureMap
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="getter">Value getter.</param>
        public FeatureMap(Func<TFeature, object> getter)
            : base(typeof(TFeature), feature => getter((TFeature)feature))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory)
            : base(typeof(TFeature), feature => getter((TFeature)feature), defaultFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        public FeatureMap(Func<TFeature, object> getter, Action<TFeature, object> setter)
            : base(typeof(TFeature), feature => getter((TFeature)feature), (feature, value) => setter((TFeature)feature, value))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory, Action<TFeature, object> setter)
            : base(typeof(TFeature), feature => getter((TFeature)feature), defaultFactory, (feature, value) => setter((TFeature)feature, value))
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="FeatureMap"/> for the specified feature interface type.
        /// </summary>
        /// <param name="getter">Value getter delegate.</param>
        /// <param name="defaultFactory">Default value factory delegate.</param>
        /// <param name="setter">Value setter delegate.</param>
        /// <param name="featureFactory">Feature factory delegate.</param>
        public FeatureMap(Func<TFeature, object> getter, Func<object> defaultFactory, Action<TFeature, object> setter, Func<TFeature> featureFactory)
            : base(typeof(TFeature), feature => getter((TFeature)feature), defaultFactory, (feature, value) => setter((TFeature)feature, value), () => featureFactory())
        {
        }
    }
}
