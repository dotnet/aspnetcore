// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal.PatternSegments;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite
{
    public static class ServerVariables
    {
        /// <summary>
        /// Returns the matching <see cref="PatternSegment"/> for the given <paramref name="serverVariable"/>
        /// </summary>
        /// <param name="serverVariable">The server variable</param>
        /// <param name="context">The parser context which is utilized when an exception is thrown</param>
        /// <param name="uriMatchPart">Indicates whether the full URI or the path should be evaluated for URL segments</param>
        /// <param name="useNativeIISServerVariables">Determines whether server variables are sourced natively from IIS</param>
        /// <exception cref="FormatException">Thrown when the server variable is unknown</exception>
        /// <returns>The matching <see cref="PatternSegment"/></returns>
        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context, UriMatchPart uriMatchPart, bool useNativeIISServerVariables)
        {
            Func<PatternSegment> fallback = default;

            switch (serverVariable)
            {
                // TODO Add all server variables here.
                case "ALL_RAW":
                    fallback = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "APP_POOL_ID":
                    fallback = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "CONTENT_LENGTH":
                    fallback = () => new HeaderSegment(HeaderNames.ContentLength);
                    break;
                case "CONTENT_TYPE":
                    fallback = () => new HeaderSegment(HeaderNames.ContentType);
                    break;
                case "HTTP_ACCEPT":
                    fallback = () => new HeaderSegment(HeaderNames.Accept);
                    break;
                case "HTTP_COOKIE":
                    fallback = () => new HeaderSegment(HeaderNames.Cookie);
                    break;
                case "HTTP_HOST":
                    fallback = () => new HeaderSegment(HeaderNames.Host);
                    break;
                case "HTTP_REFERER":
                    fallback = () => new HeaderSegment(HeaderNames.Referer);
                    break;
                case "HTTP_USER_AGENT":
                    fallback = () => new HeaderSegment(HeaderNames.UserAgent);
                    break;
                case "HTTP_CONNECTION":
                    fallback = () => new HeaderSegment(HeaderNames.Connection);
                    break;
                case "HTTP_URL":
                    fallback = () => new UrlSegment(uriMatchPart);
                    break;
                case "HTTPS":
                    fallback = () => new IsHttpsUrlSegment();
                    break;
                case "LOCAL_ADDR":
                    fallback = () => new LocalAddressSegment();
                    break;
                case "HTTP_PROXY_CONNECTION":
                    fallback = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "QUERY_STRING":
                    fallback = () => new QueryStringSegment();
                    break;
                case "REMOTE_ADDR":
                    fallback = () => new RemoteAddressSegment();
                    break;
                case "REMOTE_HOST":
                    fallback = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "REMOTE_PORT":
                    fallback = () => new RemotePortSegment();
                    break;
                case "REQUEST_FILENAME":
                    fallback = () => new RequestFileNameSegment();
                    break;
                case "REQUEST_METHOD":
                    fallback = () => new RequestMethodSegment();
                    break;
                case "REQUEST_URI":
                    fallback = () => new UrlSegment(uriMatchPart);
                    break;
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(serverVariable, context.Index));
            }

            if (!useNativeIISServerVariables)
            {
                return fallback();
            }

            return new IISServerVariableSegment(serverVariable, fallback);
        }
    }
}
