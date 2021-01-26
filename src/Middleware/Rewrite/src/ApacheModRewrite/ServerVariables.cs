// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    /// <summary>
    /// mod_rewrite lookups for specific string constants.
    /// </summary>
    internal static class ServerVariables
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
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "CONN_REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "CONTEXT_PREFIX":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "CONTEXT_DOCUMENT_ROOT":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "IPV6":
                    return new IsIPV6Segment();
                case "PATH_INFO":
                    throw new NotImplementedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "QUERY_STRING":
                    return new QueryStringSegment();
                case "REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "REMOTE_HOST":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "REMOTE_IDENT":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "REMOTE_PORT":
                    return new RemotePortSegment();
                case "REMOTE_USER":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "REQUEST_METHOD":
                    return new RequestMethodSegment();
                case "SCRIPT_FILENAME":
                    return new RequestFileNameSegment();
                case "DOCUMENT_ROOT":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "SCRIPT_GROUP":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "SCRIPT_USER":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "SERVER_ADDR":
                    return new LocalAddressSegment();
                case "SERVER_ADMIN":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "SERVER_NAME":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "SERVER_PORT":
                    return new LocalPortSegment();
                case "SERVER_PROTOCOL":
                    return new ServerProtocolSegment();
                case "SERVER_SOFTWARE":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
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
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "HTTPS":
                    return new IsHttpsModSegment();
                case "HTTP2":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "IS_SUBREQ":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                case "REQUEST_FILENAME":
                    return new RequestFileNameSegment();
                case "REQUEST_SCHEME":
                    return new SchemeSegment();
                case "REQUEST_URI":
                    return new UrlSegment();
                case "THE_REQUEST":
                    throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(serverVariable, context.Index));
            }
        }
    }
}
