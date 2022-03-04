// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.PatternSegments;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal static class ServerVariables
    {
        /// <summary>
        /// Returns the matching <see cref="PatternSegment"/> for the given <paramref name="serverVariable"/>
        /// </summary>
        /// <param name="serverVariable">The server variable</param>
        /// <param name="context">The parser context which is utilized when an exception is thrown</param>
        /// <param name="uriMatchPart">Indicates whether the full URI or the path should be evaluated for URL segments</param>
        /// <param name="alwaysUseManagedServerVariables">Determines whether server variables are sourced from the managed server</param>
        /// <exception cref="FormatException">Thrown when the server variable is unknown</exception>
        /// <returns>The matching <see cref="PatternSegment"/></returns>
        public static PatternSegment FindServerVariable(string serverVariable, ParserContext context, UriMatchPart uriMatchPart, bool alwaysUseManagedServerVariables)
        {
            Func<PatternSegment> managedVariableThunk = default;

            switch (serverVariable)
            {
                // TODO Add all server variables here.
                case "ALL_RAW":
                    managedVariableThunk = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "APP_POOL_ID":
                    managedVariableThunk = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "CONTENT_LENGTH":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.ContentLength);
                    break;
                case "CONTENT_TYPE":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.ContentType);
                    break;
                case "HTTP_ACCEPT":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.Accept);
                    break;
                case "HTTP_COOKIE":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.Cookie);
                    break;
                case "HTTP_HOST":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.Host);
                    break;
                case "HTTP_REFERER":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.Referer);
                    break;
                case "HTTP_USER_AGENT":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.UserAgent);
                    break;
                case "HTTP_CONNECTION":
                    managedVariableThunk = () => new HeaderSegment(HeaderNames.Connection);
                    break;
                case "HTTP_URL":
                    managedVariableThunk = () => new UrlSegment(uriMatchPart);
                    break;
                case "HTTPS":
                    managedVariableThunk = () => new IsHttpsUrlSegment();
                    break;
                case "LOCAL_ADDR":
                    managedVariableThunk = () => new LocalAddressSegment();
                    break;
                case "HTTP_PROXY_CONNECTION":
                    managedVariableThunk = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "QUERY_STRING":
                    managedVariableThunk = () => new QueryStringSegment();
                    break;
                case "REMOTE_ADDR":
                    managedVariableThunk = () => new RemoteAddressSegment();
                    break;
                case "REMOTE_HOST":
                    managedVariableThunk = () => throw new NotSupportedException(Resources.FormatError_UnsupportedServerVariable(serverVariable));
                    break;
                case "REMOTE_PORT":
                    managedVariableThunk = () => new RemotePortSegment();
                    break;
                case "REQUEST_FILENAME":
                    managedVariableThunk = () => new RequestFileNameSegment();
                    break;
                case "REQUEST_METHOD":
                    managedVariableThunk = () => new RequestMethodSegment();
                    break;
                case "REQUEST_URI":
                    managedVariableThunk = () => new UrlSegment(uriMatchPart);
                    break;
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(serverVariable, context.Index));
            }

            if (alwaysUseManagedServerVariables)
            {
                return managedVariableThunk();
            }

            return new IISServerVariableSegment(serverVariable, managedVariableThunk);
        }
    }
}
