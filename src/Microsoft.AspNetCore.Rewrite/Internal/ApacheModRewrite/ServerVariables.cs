// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.ApacheModRewrite
{
    /// <summary>
    /// mod_rewrite lookups for specific string constants.
    /// </summary>
    public static class ServerVariables
    {

        /// <summary>
        /// Translates mod_rewrite server variables strings to an enum of different server variables.
        /// </summary>
        /// <param name="serverVariable">The server variable string.</param>
        /// <param name="context">The Parser context</param>
        /// <returns>The appropriate enum if the server variable exists, else ServerVariable.None</returns>
        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context)
        {
            switch (serverVariable)
            {
                case "HTTP_ACCEPT":
                    return new HeaderSegment(HeaderNames.Accept);
                case "HTTP_COOKIE":
                    return new HeaderSegment(HeaderNames.Cookie);
                case "HTTP_HOST":
                    return new HeaderSegment(HeaderNames.Host);
                case "HTTP_REFERER":
                    return new HeaderSegment(HeaderNames.Referer);
                case "HTTP_USER_AGENT":
                    return new HeaderSegment(HeaderNames.UserAgent);
                case "HTTP_CONNECTION":
                    return new HeaderSegment(HeaderNames.Connection);
                case "HTTP_FORWARDED":
                    return new HeaderSegment("Forwarded");
                case "AUTH_TYPE":
                    throw new NotSupportedException("Rules using the AUTH_TYPE server variable are not supported");
                case "CONN_REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "CONTEXT_PREFIX":
                    throw new NotSupportedException("Rules using the CONTEXT_PREFIX server variable are not supported");
                case "CONTEXT_DOCUMENT_ROOT":
                    throw new NotSupportedException("Rules using the CONTEXT_DOCUMENT_ROOT server variable are not supported");
                case "IPV6":
                    return new IsIPV6Segment();
                case "PATH_INFO":
                    throw new NotImplementedException("Rules using the PATH_INFO server variable are not implemented");
                case "QUERY_STRING":
                    return new QueryStringSegment();
                case "REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "REMOTE_HOST":
                    throw new NotSupportedException("Rules using the REMOTE_HOST server variable are not supported");
                case "REMOTE_IDENT":
                    throw new NotSupportedException("Rules using the REMOTE_IDENT server variable are not supported");
                case "REMOTE_PORT":
                    return new RemotePortSegment();
                case "REMOTE_USER":
                    throw new NotSupportedException("Rules using the REMOTE_USER server variable are not supported");
                case "REQUEST_METHOD":
                    return new RequestMethodSegment();
                case "SCRIPT_FILENAME":
                    return new RequestFileNameSegment();
                case "DOCUMENT_ROOT":
                    throw new NotSupportedException("Rules using the DOCUMENT_ROOT server variable are not supported");
                case "SCRIPT_GROUP":
                    throw new NotSupportedException("Rules using the SCRIPT_GROUP server variable are not supported");
                case "SCRIPT_USER":
                    throw new NotSupportedException("Rules using the SCRIPT_USER server variable are not supported");
                case "SERVER_ADDR":
                    return new LocalAddressSegment();
                case "SERVER_ADMIN":
                    throw new NotSupportedException("Rules using the SERVER_ADMIN server variable are not supported");
                case "SERVER_NAME":
                    throw new NotSupportedException("Rules using the SERVER_NAME server variable are not supported");
                case "SERVER_PORT":
                    return new LocalPortSegment();
                case "SERVER_PROTOCOL":
                    return new ServerProtocolSegment();
                case "SERVER_SOFTWARE":
                    throw new NotSupportedException("Rules using the SERVER_SOFTWARE server variable are not supported");
                case "TIME_YEAR":
                    return new DateTimeSegment(serverVariable);
                case "TIME_MON":
                    return new DateTimeSegment(serverVariable);
                case "TIME_DAY":
                    return new DateTimeSegment(serverVariable);
                case "TIME_HOUR":
                    return new DateTimeSegment(serverVariable);
                case "TIME_MIN":
                    return new DateTimeSegment(serverVariable);
                case "TIME_SEC":
                    return new DateTimeSegment(serverVariable);
                case "TIME_WDAY":
                    return new DateTimeSegment(serverVariable);
                case "TIME":
                    return new DateTimeSegment(serverVariable);
                case "API_VERSION":
                    throw new NotSupportedException("Rules using the API_VERSION server variable are not supported");
                case "HTTPS":
                    return new IsHttpsModSegment();
                case "HTTP2":
                    throw new NotSupportedException("Rules using the HTTP2 server variable are not supported");
                case "IS_SUBREQ":
                    throw new NotSupportedException("Rules using the IS_SUBREQ server variable are not supported");
                case "REQUEST_FILENAME":
                    return new RequestFileNameSegment();
                case "REQUEST_SCHEME":
                    return new SchemeSegment();
                case "REQUEST_URI":
                    return new UrlSegment();
                case "THE_REQUEST":
                    throw new NotSupportedException("Rules using the THE_REQUEST server variable are not supported");
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(serverVariable, context.Index));
            }
        }
    }
}
