// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// mod_rewrite lookups for specific string constants.
    /// </summary>
    public static class ServerVariables
    {

        /// <summary>
        /// Translates mod_rewrite server variables strings to an enum of different server variables.
        /// </summary>
        /// <param name="variable">The server variable string.</param>
        /// <param name="context">The Parser context</param>
        /// <returns>The appropriate enum if the server variable exists, else ServerVariable.None</returns>
        public static PatternSegment FindServerVariable(string variable, ParserContext context)
        {
            switch (variable)
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
                    throw new NotImplementedException("Auth-Type server variable is not supported");
                case "CONN_REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "CONTEXT_PREFIX":
                    throw new NotImplementedException("Context-prefix server variable is not supported");
                case "CONTEXT_DOCUMENT_ROOT":
                    throw new NotImplementedException("Context-Document-Root server variable is not supported");
                case "IPV6":
                    return new IsIPV6Segment();
                case "PATH_INFO":
                    throw new NotImplementedException("Path-Info server variable is not supported");
                case "QUERY_STRING":
                    return new QueryStringSegment();
                case "REMOTE_ADDR":
                    return new RemoteAddressSegment();
                case "REMOTE_HOST":
                    throw new NotImplementedException("Remote-Host server variable is not supported");
                case "REMOTE_IDENT":
                    throw new NotImplementedException("Remote-Identity server variable is not supported");
                case "REMOTE_PORT":
                    return new RemotePortSegment();
                case "REMOTE_USER":
                    throw new NotImplementedException("Remote-User server variable is not supported");
                case "REQUEST_METHOD":
                    return new RequestMethodSegment();
                case "SCRIPT_FILENAME":
                    throw new NotImplementedException("Script-Filename server variable is not supported");
                case "DOCUMENT_ROOT":
                    throw new NotImplementedException("Document-Root server variable is not supported");
                case "SCRIPT_GROUP":
                    throw new NotImplementedException("Script-Group server variable is not supported");
                case "SCRIPT_USER":
                    throw new NotImplementedException("Script-User server variable is not supported");
                case "SERVER_ADDR":
                    return new LocalAddressSegment();
                case "SERVER_ADMIN":
                    throw new NotImplementedException("Server-Admin server variable is not supported");
                case "SERVER_NAME":
                    throw new NotImplementedException("Server-Name server variable is not supported");
                case "SERVER_PORT":
                    return new LocalPortSegment();
                case "SERVER_PROTOCOL":
                    return new ServerProtocolSegment();
                case "SERVER_SOFTWARE":
                    throw new NotImplementedException("Server-Software server variable is not supported");
                case "TIME_YEAR":
                    return new DateTimeSegment(variable);
                case "TIME_MON":
                    return new DateTimeSegment(variable);
                case "TIME_DAY":
                    return new DateTimeSegment(variable);
                case "TIME_HOUR":
                    return new DateTimeSegment(variable);
                case "TIME_MIN":
                    return new DateTimeSegment(variable);
                case "TIME_SEC":
                    return new DateTimeSegment(variable);
                case "TIME_WDAY":
                    return new DateTimeSegment(variable);
                case "TIME":
                    return new DateTimeSegment(variable);
                case "API_VERSION":
                    throw new NotImplementedException();
                case "HTTPS":
                    return new IsHttpsModSegment();
                case "HTTP2":
                    throw new NotImplementedException("Http2 server variable is not supported");
                case "IS_SUBREQ":
                    throw new NotImplementedException("Is-Subrequest server variable is not supported");
                case "REQUEST_FILENAME":
                    return new RequestFileNameSegment();
                case "REQUEST_SCHEME":
                    return new SchemeSegment();
                case "REQUEST_URI":
                    return new UrlSegment();
                case "THE_REQUEST":
                    throw new NotImplementedException("The-Request server variable is not supported");
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(variable, context.Index));
            }
        }
    }
}
