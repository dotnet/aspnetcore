//------------------------------------------------------------------------------
// <copyright file="HttpStatusCode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Net.Server
{
    // Redirect Status code numbers that need to be defined.

    /// <devdoc>
    ///    <para>Contains the values of status
    ///       codes defined for the HTTP protocol.</para>
    /// </devdoc>
    // UEUE : Any int can be cast to a HttpStatusCode to allow checking for non http1.1 codes.
    internal enum HttpStatusCode
    {
        // Informational 1xx

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Continue = 100,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SwitchingProtocols = 101,

        // Successful 2xx

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        OK = 200,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Created = 201,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Accepted = 202,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NonAuthoritativeInformation = 203,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NoContent = 204,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ResetContent = 205,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PartialContent = 206,

        // Redirection 3xx

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MultipleChoices = 300,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Ambiguous = 300,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MovedPermanently = 301,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Moved = 301,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Found = 302,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Redirect = 302,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        SeeOther = 303,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RedirectMethod = 303,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NotModified = 304,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        UseProxy = 305,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unused = 306,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        TemporaryRedirect = 307,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RedirectKeepVerb = 307,

        // Client Error 4xx

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BadRequest = 400,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Unauthorized = 401,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PaymentRequired = 402,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Forbidden = 403,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NotFound = 404,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        MethodNotAllowed = 405,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NotAcceptable = 406,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ProxyAuthenticationRequired = 407,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RequestTimeout = 408,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Conflict = 409,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Gone = 410,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        LengthRequired = 411,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        PreconditionFailed = 412,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RequestEntityTooLarge = 413,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RequestUriTooLong = 414,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        UnsupportedMediaType = 415,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        RequestedRangeNotSatisfiable = 416,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ExpectationFailed = 417,

        UpgradeRequired = 426,

        // Server Error 5xx

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        InternalServerError = 500,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        NotImplemented = 501,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        BadGateway = 502,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        ServiceUnavailable = 503,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        GatewayTimeout = 504,

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        HttpVersionNotSupported = 505,
    } // enum HttpStatusCode

    /*
    Fielding, et al.            Standards Track                     [Page 3]

    RFC 2616                        HTTP/1.1                       June 1999


       10.1  Informational 1xx ...........................................57
       10.1.1   100 Continue .............................................58
       10.1.2   101 Switching Protocols ..................................58
       10.2  Successful 2xx ..............................................58
       10.2.1   200 OK ...................................................58
       10.2.2   201 Created ..............................................59
       10.2.3   202 Accepted .............................................59
       10.2.4   203 Non-Authoritative Information ........................59
       10.2.5   204 No Content ...........................................60
       10.2.6   205 Reset Content ........................................60
       10.2.7   206 Partial Content ......................................60
       10.3  Redirection 3xx .............................................61
       10.3.1   300 Multiple Choices .....................................61
       10.3.2   301 Moved Permanently ....................................62
       10.3.3   302 Found ................................................62
       10.3.4   303 See Other ............................................63
       10.3.5   304 Not Modified .........................................63
       10.3.6   305 Use Proxy ............................................64
       10.3.7   306 (Unused) .............................................64
       10.3.8   307 Temporary Redirect ...................................65
       10.4  Client Error 4xx ............................................65
       10.4.1    400 Bad Request .........................................65
       10.4.2    401 Unauthorized ........................................66
       10.4.3    402 Payment Required ....................................66
       10.4.4    403 Forbidden ...........................................66
       10.4.5    404 Not Found ...........................................66
       10.4.6    405 Method Not Allowed ..................................66
       10.4.7    406 Not Acceptable ......................................67
       10.4.8    407 Proxy Authentication Required .......................67
       10.4.9    408 Request Timeout .....................................67
       10.4.10   409 Conflict ............................................67
       10.4.11   410 Gone ................................................68
       10.4.12   411 Length Required .....................................68
       10.4.13   412 Precondition Failed .................................68
       10.4.14   413 Request Entity Too Large ............................69
       10.4.15   414 Request-URI Too Long ................................69
       10.4.16   415 Unsupported Media Type ..............................69
       10.4.17   416 Requested Range Not Satisfiable .....................69
       10.4.18   417 Expectation Failed ..................................70
       10.5  Server Error 5xx ............................................70
       10.5.1   500 Internal Server Error ................................70
       10.5.2   501 Not Implemented ......................................70
       10.5.3   502 Bad Gateway ..........................................70
       10.5.4   503 Service Unavailable ..................................70
       10.5.5   504 Gateway Timeout ......................................71
       10.5.6   505 HTTP Version Not Supported ...........................71
    */
} // namespace System.Net
