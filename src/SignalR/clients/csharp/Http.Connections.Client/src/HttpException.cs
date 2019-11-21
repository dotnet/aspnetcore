using System;
using System.Net;

namespace Microsoft.AspNetCore.Http.Connections.Client
{
    /// <summary>
    /// Base Http Exception
    /// </summary>
    public class HttpException : Exception
    {
        /// <summary>
        /// HTTP Status Code for this exception
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Create an exception with a HTTP status code
        /// </summary>
        /// <param name="httpStatusCode">HTTP Status Code</param>
        public HttpException(HttpStatusCode httpStatusCode)
        {
            StatusCode = httpStatusCode;
        }

        /// <summary>
        /// Create an exception with a HTTP status code and message
        /// </summary>
        /// <param name="httpStatusCode">HTTP Status Code</param>
        /// <param name="message">Error description</param>
        public HttpException(HttpStatusCode httpStatusCode, string message)
        : base(message)
        {
            StatusCode = httpStatusCode;
        }

        /// <summary>
        /// Create an exception with a HTTP status code and message. Also set 'inner' as the inner exception.
        /// </summary>
        /// <param name="httpStatusCode">HTTP Status Code</param>
        /// <param name="message">Error description</param>
        /// <param name="inner"></param>
        public HttpException(HttpStatusCode httpStatusCode, string message, Exception inner)
        : base(message, inner)
        {
            StatusCode = httpStatusCode;
        }

        /// <summary>
        /// ToString override produces 'StatusCode - Message'
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{(int)StatusCode} - {Message}";
        }
    }
}
