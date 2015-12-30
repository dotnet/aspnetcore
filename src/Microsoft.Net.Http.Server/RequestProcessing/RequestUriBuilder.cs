// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="RequestUriBuilder.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Net.Http.Server
{
    // We don't use the cooked URL because http.sys unescapes all percent-encoded values. However,
    // we also can't just use the raw Uri, since http.sys supports not only Utf-8, but also ANSI/DBCS and
    // Unicode code points. System.Uri only supports Utf-8.
    // The purpose of this class is to decode all UTF-8 percent encoded characters, with the
    // exception of %2F ('/'), which is left encoded.
    internal sealed class RequestUriBuilder
    {
        private static readonly Encoding Utf8Encoding;

        private readonly string _rawUri;
        private readonly string _cookedUriPath;

        // This field is used to build the final request Uri string from the Uri parts passed to the ctor.
        private StringBuilder _requestUriString;

        // The raw path is parsed by looping through all characters from left to right. 'rawOctets'
        // is used to store consecutive percent encoded octets as actual byte values: e.g. for path /pa%C3%84th%20/
        // rawOctets will be set to { 0xC3, 0x84 } when we reach character 't' and it will be { 0x20 } when
        // we reach the final '/'. I.e. after a sequence of percent encoded octets ends, we use rawOctets as 
        // input to the encoding and decode them into a string.
        private List<byte> _rawOctets;
        private string _rawPath;

        private ILogger _logger;

        static RequestUriBuilder()
        {
            Utf8Encoding = new UTF8Encoding(false, true);
        }

        private RequestUriBuilder(string rawUri, string cookedUriPath, ILogger logger)
        {
            Debug.Assert(!string.IsNullOrEmpty(rawUri), "Empty raw URL.");
            Debug.Assert(!string.IsNullOrEmpty(cookedUriPath), "Empty cooked URL path.");
            Debug.Assert(logger != null, "Null logger.");

            this._rawUri = rawUri;
            this._cookedUriPath = AddSlashToAsteriskOnlyPath(cookedUriPath);
            this._logger = logger;
        }

        private enum ParsingResult
        {
            Success,
            InvalidString,
            EncodingError
        }

        // Process only the path.
        internal static string GetRequestPath(string rawUri, string cookedUriPath, ILogger logger)
        {
            RequestUriBuilder builder = new RequestUriBuilder(rawUri, cookedUriPath, logger);

            return builder.GetPath();
        }

        private string GetPath()
        {
            // Initialize 'rawPath' only if really needed; i.e. if we build the request Uri from the raw Uri.
            _rawPath = GetPath(_rawUri);

            // If HTTP.sys only parses Utf-8, we can safely use the raw path: it must be a valid Utf-8 string.
            if (!HttpSysSettings.EnableNonUtf8 || string.IsNullOrEmpty(_rawPath))
            {
                if (string.IsNullOrEmpty(_rawPath))
                {
                    _rawPath = "/";
                }
                return _rawPath;
            }

            _rawOctets = new List<byte>();
            _requestUriString = new StringBuilder();
            ParsingResult result = ParseRawPath(Utf8Encoding);
            
            if (result == ParsingResult.Success)
            {
                return _requestUriString.ToString();
            }

            // Fallback
            return _cookedUriPath;
        }

        private ParsingResult ParseRawPath(Encoding encoding)
        {
            Debug.Assert(encoding != null, "'encoding' must be assigned.");

            int index = 0;
            char current = '\0';
            while (index < _rawPath.Length)
            {
                current = _rawPath[index];
                if (current == '%')
                {
                    // Assert is enough, since http.sys accepted the request string already. This should never happen.
                    Debug.Assert(index + 2 < _rawPath.Length, "Expected at least 2 characters after '%' (e.g. %20)");

                    // We have a percent encoded octet: %XX
                    var octetString = _rawPath.Substring(index + 1, 2);

                    // Leave %2F as is, otherwise add to raw octets list for unescaping
                    if (octetString == "2F" || octetString == "2f")
                    {
                        _requestUriString.Append('%');
                        _requestUriString.Append(octetString);
                    }
                    else if (!AddPercentEncodedOctetToRawOctetsList(encoding, octetString))
                    {
                        return ParsingResult.InvalidString;
                    }

                    index += 3;
                }
                else
                {
                    if (!EmptyDecodeAndAppendDecodedOctetsList(encoding))
                    {
                        return ParsingResult.EncodingError;
                    }

                    // Append the current character to the result.
                    _requestUriString.Append(current);
                    index++;
                }
            }

            // if the raw path ends with a sequence of percent encoded octets, make sure those get added to the
            // result (requestUriString).
            if (!EmptyDecodeAndAppendDecodedOctetsList(encoding))
            {
                return ParsingResult.EncodingError;
            }

            return ParsingResult.Success;
        }

        private bool AddPercentEncodedOctetToRawOctetsList(Encoding encoding, string escapedCharacter)
        {
            byte encodedValue;
            if (!byte.TryParse(escapedCharacter, NumberStyles.HexNumber, null, out encodedValue))
            {
                LogHelper.LogDebug(_logger, nameof(AddPercentEncodedOctetToRawOctetsList), "Can't convert code point: " + escapedCharacter);
                return false;
            }

            _rawOctets.Add(encodedValue);

            return true;
        }

        private bool EmptyDecodeAndAppendDecodedOctetsList(Encoding encoding)
        {
            if (_rawOctets.Count == 0)
            {
                return true;
            }

            string decodedString = null;
            try
            {
                // If the encoding can get a string out of the byte array, this is a valid string in the
                // 'encoding' encoding.
                var bytes = _rawOctets.ToArray();
                decodedString = encoding.GetString(bytes, 0, bytes.Length);

                _requestUriString.Append(decodedString);
                _rawOctets.Clear();

                return true;
            }
            catch (DecoderFallbackException e)
            {
                LogHelper.LogDebug(_logger, nameof(EmptyDecodeAndAppendDecodedOctetsList), "Can't convert bytes: " + GetOctetsAsString(_rawOctets) + ": " + e.Message);
            }

            return false;
        }

        private static string GetOctetsAsString(IEnumerable<byte> octets)
        {
            StringBuilder octetString = new StringBuilder();

            bool first = true;
            foreach (byte octet in octets)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    octetString.Append(" ");
                }
                octetString.Append(octet.ToString("X2", CultureInfo.InvariantCulture));
            }

            return octetString.ToString();
        }

        private static string GetPath(string uriString)
        {
            Debug.Assert(uriString != null, "uriString must not be null");
            Debug.Assert(uriString.Length > 0, "uriString must not be empty");

            int pathStartIndex = 0;

            // Perf. improvement: nearly all strings are relative Uris. So just look if the
            // string starts with '/'. If so, we have a relative Uri and the path starts at position 0.
            // (http.sys already trimmed leading whitespaces)
            if (uriString[0] != '/')
            {
                // We can't check against cookedUriScheme, since http.sys allows for request http://myserver/ to
                // use a request line 'GET https://myserver/' (note http vs. https). Therefore check if the
                // Uri starts with either http:// or https://.
                int authorityStartIndex = 0;
                if (uriString.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    authorityStartIndex = 7;
                }
                else if (uriString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    authorityStartIndex = 8;
                }

                if (authorityStartIndex > 0)
                {
                    // we have an absolute Uri. Find out where the authority ends and the path begins.
                    // Note that Uris like "http://server?query=value/1/2" are invalid according to RFC2616
                    // and http.sys behavior: If the Uri contains a query, there must be at least one '/'
                    // between the authority and the '?' character: It's safe to just look for the first
                    // '/' after the authority to determine the beginning of the path.
                    pathStartIndex = uriString.IndexOf('/', authorityStartIndex);
                    if (pathStartIndex == -1)
                    {
                        // e.g. for request lines like: 'GET http://myserver' (no final '/')
                        pathStartIndex = uriString.Length;
                    }
                }
                else
                {
                    // RFC2616: Request-URI = "*" | absoluteURI | abs_path | authority
                    // 'authority' can only be used with CONNECT which is never received by HttpListener.
                    // I.e. if we don't have an absolute path (must start with '/') and we don't have
                    // an absolute Uri (must start with http:// or https://), then 'uriString' must be '*'.
                    Debug.Assert((uriString.Length == 1) && (uriString[0] == '*'), "Unknown request Uri string format; "
                        + "Request Uri string is not an absolute Uri, absolute path, or '*': " + uriString);

                    // Should we ever get here, be consistent with 2.0/3.5 behavior: just add an initial
                    // slash to the string and treat it as a path:
                    uriString = "/" + uriString;
                }
            }

            // Find end of path: The path is terminated by
            // - the first '?' character
            // - the first '#' character: This is never the case here, since http.sys won't accept 
            //   Uris containing fragments. Also, RFC2616 doesn't allow fragments in request Uris.
            // - end of Uri string
            int queryIndex = uriString.IndexOf('?');
            if (queryIndex == -1)
            {
                queryIndex = uriString.Length;
            }

            // will always return a != null string.
            return AddSlashToAsteriskOnlyPath(uriString.Substring(pathStartIndex, queryIndex - pathStartIndex));
        }

        private static string AddSlashToAsteriskOnlyPath(string path)
        {
            Debug.Assert(path != null, "'path' must not be null");

            // If a request like "OPTIONS * HTTP/1.1" is sent to the listener, then the request Uri
            // should be "http[s]://server[:port]/*" to be compatible with pre-4.0 behavior.
            if ((path.Length == 1) && (path[0] == '*'))
            {
                return "/*";
            }

            return path;
        }
    }
}
