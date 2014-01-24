using System;
using System.IO;
using System.Threading;

namespace Microsoft.AspNet.Abstractions
{
    public abstract class HttpRequest
    {
        // TODO - review IOwinRequest for properties

        public abstract HttpContext HttpContext { get; }

        /// <summary>
        /// Gets or set the HTTP method.
        /// </summary>
        /// <returns>The HTTP method.</returns>
        public abstract string Method { get; set; }

        /// <summary>
        /// Gets or set the HTTP request scheme from owin.RequestScheme.
        /// </summary>
        /// <returns>The HTTP request scheme from owin.RequestScheme.</returns>
        public abstract string Scheme { get; set; }

        /// <summary>
        /// Returns true if the owin.RequestScheme is https.
        /// </summary>
        /// <returns>true if this request is using https; otherwise, false.</returns>
        public abstract bool IsSecure { get; }

        /// <summary>
        /// Gets or set the Host header. May include the port.
        /// </summary>
        /// <return>The Host header.</return>
        public abstract HostString Host { get; set; }

        /// <summary>
        /// Gets or set the owin.RequestPathBase.
        /// </summary>
        /// <returns>The owin.RequestPathBase.</returns>
        public abstract PathString PathBase { get; set; }

        /// <summary>
        /// Gets or set the request path from owin.RequestPath.
        /// </summary>
        /// <returns>The request path from owin.RequestPath.</returns>
        public abstract PathString Path { get; set; }

        /// <summary>
        /// Gets or set the query string from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query string from owin.RequestQueryString.</returns>
        public abstract QueryString QueryString { get; set; }

        /// <summary>
        /// Gets the query value collection parsed from owin.RequestQueryString.
        /// </summary>
        /// <returns>The query value collection parsed from owin.RequestQueryString.</returns>
        public abstract IReadableStringCollection Query { get; }

        /// <summary>
        /// Gets the uniform resource identifier (URI) associated with the request.
        /// </summary>
        /// <returns>The uniform resource identifier (URI) associated with the request.</returns>
        public abstract Uri Uri { get; }

        /// <summary>
        /// Gets or set the owin.RequestProtocol.
        /// </summary>
        /// <returns>The owin.RequestProtocol.</returns>
        public abstract string Protocol { get; set; }

        /// <summary>
        /// Gets the request headers.
        /// </summary>
        /// <returns>The request headers.</returns>
        public abstract IHeaderDictionary Headers { get; }

        /// <summary>
        /// Gets the collection of Cookies for this request.
        /// </summary>
        /// <returns>The collection of Cookies for this request.</returns>
        public abstract IReadableStringCollection Cookies { get; }

        /// <summary>
        /// Gets or sets the Content-Type header.
        /// </summary>
        /// <returns>The Content-Type header.</returns>
        // (TODO header conventions?) public abstract string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the Cache-Control header.
        /// </summary>
        /// <returns>The Cache-Control header.</returns>
        // (TODO header conventions?) public abstract string CacheControl { get; set; }

        /// <summary>
        /// Gets or sets the Media-Type header.
        /// </summary>
        /// <returns>The Media-Type header.</returns>
        // (TODO header conventions?) public abstract string MediaType { get; set; }

        /// <summary>
        /// Gets or set the Accept header.
        /// </summary>
        /// <returns>The Accept header.</returns>
        // (TODO header conventions?) public abstract string Accept { get; set; }

        /// <summary>
        /// Gets or set the owin.RequestBody Stream.
        /// </summary>
        /// <returns>The owin.RequestBody Stream.</returns>
        public abstract Stream Body { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token for the request.
        /// </summary>
        /// <returns>The cancellation token for the request.</returns>
        public abstract CancellationToken CallCanceled { get; set; }

    }
}
