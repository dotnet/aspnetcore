// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class HttpParsingData
    {
        public static IEnumerable<string[]> ValidRequestLineData
        {
            get
            {
                var methods = new[]
                {
                    "GET",
                    "CUSTOM",
                };
                var targets = new[]
                {
                    Tuple.Create("/", "/"),
                    Tuple.Create("/abc", "/abc"),
                    Tuple.Create("/abc/de/f", "/abc/de/f"),
                    Tuple.Create("/%20", "/ "),
                    Tuple.Create("/a%20", "/a "),
                    Tuple.Create("/%20a", "/ a"),
                    Tuple.Create("/a/b%20c", "/a/b c"),
                    Tuple.Create("/%C3%A5", "/\u00E5"),
                    Tuple.Create("/a%C3%A5a", "/a\u00E5a"),
                    Tuple.Create("/%C3%A5/bc", "/\u00E5/bc"),
                    Tuple.Create("/%25", "/%"),
                    Tuple.Create("/%2F", "/%2F"),
                };
                var queryStrings = new[]
                {
                    "",
                    "?",
                    "?arg1=val1",
                    "?arg1=a%20b",
                    "?%A",
                    "?%20=space",
                    "?%C3%A5=val",
                    "?path=/home",
                    "?path=/%C3%A5/",
                    "?question=what?",
                    "?%00",
                    "?arg=%00"
                };
                var httpVersions = new[]
                {
                    "HTTP/1.0",
                    "HTTP/1.1"
                };

                return from method in methods
                       from target in targets
                       from queryString in queryStrings
                       from httpVersion in httpVersions
                       select new[]
                       {
                           $"{method} {target.Item1}{queryString} {httpVersion}\r\n",
                           method,
                           $"{target.Item2}",
                           queryString,
                           httpVersion
                       };
            }
        }

        public static IEnumerable<string> InvalidRequestLineData => new[]
        {
            "G\r\n",
            "GE\r\n",
            "GET\r\n",
            "GET \r\n",
            "GET /\r\n",
            "GET / \r\n",
            "GET/HTTP/1.1\r\n",
            "GET /HTTP/1.1\r\n",
            " \r\n",
            "  \r\n",
            "/ HTTP/1.1\r\n",
            " / HTTP/1.1\r\n",
            "/ \r\n",
            "GET  \r\n",
            "GET  HTTP/1.0\r\n",
            "GET  HTTP/1.1\r\n",
            "GET / \n",
            "GET / HTTP/1.0\n",
            "GET / HTTP/1.1\n",
            "GET / HTTP/1.0\rA\n",
            "GET / HTTP/1.1\ra\n",
            "GET? / HTTP/1.1\r\n",
            "GET ? HTTP/1.1\r\n",
            "GET /a?b=cHTTP/1.1\r\n",
            "GET /a%20bHTTP/1.1\r\n",
            "GET /a%20b?c=dHTTP/1.1\r\n",
            "GET %2F HTTP/1.1\r\n",
            "GET %00 HTTP/1.1\r\n",
            "CUSTOM \r\n",
            "CUSTOM /\r\n",
            "CUSTOM / \r\n",
            "CUSTOM /HTTP/1.1\r\n",
            "CUSTOM  \r\n",
            "CUSTOM  HTTP/1.0\r\n",
            "CUSTOM  HTTP/1.1\r\n",
            "CUSTOM / \n",
            "CUSTOM / HTTP/1.0\n",
            "CUSTOM / HTTP/1.1\n",
            "CUSTOM / HTTP/1.0\rA\n",
            "CUSTOM / HTTP/1.1\ra\n",
            "CUSTOM ? HTTP/1.1\r\n",
            "CUSTOM /a?b=cHTTP/1.1\r\n",
            "CUSTOM /a%20bHTTP/1.1\r\n",
            "CUSTOM /a%20b?c=dHTTP/1.1\r\n",
            "CUSTOM %2F HTTP/1.1\r\n",
            "CUSTOM %00 HTTP/1.1\r\n",
            // Bad HTTP Methods (invalid according to RFC)
            "( / HTTP/1.0\r\n",
            ") / HTTP/1.0\r\n",
            "< / HTTP/1.0\r\n",
            "> / HTTP/1.0\r\n",
            "@ / HTTP/1.0\r\n",
            ", / HTTP/1.0\r\n",
            "; / HTTP/1.0\r\n",
            ": / HTTP/1.0\r\n",
            "\\ / HTTP/1.0\r\n",
            "\" / HTTP/1.0\r\n",
            "/ / HTTP/1.0\r\n",
            "[ / HTTP/1.0\r\n",
            "] / HTTP/1.0\r\n",
            "? / HTTP/1.0\r\n",
            "= / HTTP/1.0\r\n",
            "{ / HTTP/1.0\r\n",
            "} / HTTP/1.0\r\n",
            "get@ / HTTP/1.0\r\n",
            "post= / HTTP/1.0\r\n",
        };

        public static IEnumerable<string> EncodedNullCharInTargetRequestLines => new[]
        {
            "GET /%00 HTTP/1.1\r\n",
            "GET /%00%00 HTTP/1.1\r\n",
            "GET /%E8%00%84 HTTP/1.1\r\n",
            "GET /%E8%85%00 HTTP/1.1\r\n",
            "GET /%F3%00%82%86 HTTP/1.1\r\n",
            "GET /%F3%85%00%82 HTTP/1.1\r\n",
            "GET /%F3%85%82%00 HTTP/1.1\r\n",
            "GET /%E8%85%00 HTTP/1.1\r\n",
            "GET /%E8%01%00 HTTP/1.1\r\n",
        };

        public static IEnumerable<string> NullCharInTargetRequestLines => new[]
            {
                "GET \0 HTTP/1.1\r\n",
                "GET /\0 HTTP/1.1\r\n",
                "GET /\0\0 HTTP/1.1\r\n",
                "GET /%C8\0 HTTP/1.1\r\n",
            };

        public static TheoryData<string> UnrecognizedHttpVersionData => new TheoryData<string>
        {
            "H",
            "HT",
            "HTT",
            "HTTP",
            "HTTP/",
            "HTTP/1",
            "HTTP/1.",
            "http/1.0",
            "http/1.1",
            "HTTP/1.1 ",
            "HTTP/1.0a",
            "HTTP/1.0ab",
            "HTTP/1.1a",
            "HTTP/1.1ab",
            "HTTP/1.2",
            "HTTP/3.0",
            "hello",
            "8charact",
        };

        public static IEnumerable<object[]> InvalidRequestHeaderData
        {
            get
            {
                // Line folding
                var headersWithLineFolding = new[]
                {
                    "Header: line1\r\n line2\r\n\r\n",
                    "Header: line1\r\n\tline2\r\n\r\n",
                    "Header: line1\r\n  line2\r\n\r\n",
                    "Header: line1\r\n \tline2\r\n\r\n",
                    "Header: line1\r\n\t line2\r\n\r\n",
                    "Header: line1\r\n\t\tline2\r\n\r\n",
                    "Header: line1\r\n \t\t line2\r\n\r\n",
                    "Header: line1\r\n \t \t line2\r\n\r\n",
                    "Header-1: multi\r\n line\r\nHeader-2: value2\r\n\r\n",
                    "Header-1: value1\r\nHeader-2: multi\r\n line\r\n\r\n",
                    "Header-1: value1\r\n Header-2: value2\r\n\r\n",
                    "Header-1: value1\r\n\tHeader-2: value2\r\n\r\n",
                };

                // CR in value
                var headersWithCRInValue = new[]
                {
                    "Header-1: value1\r\r\n",
                    "Header-1: val\rue1\r\n",
                    "Header-1: value1\rHeader-2: value2\r\n\r\n",
                    "Header-1: value1\r\nHeader-2: value2\r\r\n",
                    "Header-1: value1\r\nHeader-2: v\ralue2\r\n",
                };

                // Missing colon
                var headersWithMissingColon = new[]
                {
                    "Header-1 value1\r\n\r\n",
                    "Header-1 value1\r\nHeader-2: value2\r\n\r\n",
                    "Header-1: value1\r\nHeader-2 value2\r\n\r\n",
                };

                // Starting with whitespace
                var headersStartingWithWhitespace = new[]
                {
                    " Header: value\r\n\r\n",
                    "\tHeader: value\r\n\r\n",
                    " Header-1: value1\r\nHeader-2: value2\r\n\r\n",
                    "\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n",
                };

                // Whitespace in header name
                var headersWithWithspaceInName = new[]
                {
                    "Header : value\r\n\r\n",
                    "Header\t: value\r\n\r\n",
                    "Header 1: value1\r\nHeader-2: value2\r\n\r\n",
                    "Header 1 : value1\r\nHeader-2: value2\r\n\r\n",
                    "Header 1\t: value1\r\nHeader-2: value2\r\n\r\n",
                    "Header-1: value1\r\nHeader 2: value2\r\n\r\n",
                    "Header-1: value1\r\nHeader-2 : value2\r\n\r\n",
                    "Header-1: value1\r\nHeader-2\t: value2\r\n\r\n",
                };

                // Headers not ending in CRLF line
                var headersNotEndingInCrLfLine = new[]
                {
                    "Header-1: value1\r\nHeader-2: value2\r\n\r\r",
                    "Header-1: value1\r\nHeader-2: value2\r\n\r ",
                    "Header-1: value1\r\nHeader-2: value2\r\n\r \n",
                };

                return new[]
                {
                    Tuple.Create(headersWithLineFolding,"Whitespace is not allowed in header name."),
                    Tuple.Create(headersWithCRInValue,"Header value must not contain CR characters."),
                    Tuple.Create(headersWithMissingColon,"No ':' character found in header line."),
                    Tuple.Create(headersStartingWithWhitespace, "Whitespace is not allowed in header name."),
                    Tuple.Create(headersWithWithspaceInName,"Whitespace is not allowed in header name."),
                    Tuple.Create(headersNotEndingInCrLfLine, "Headers corrupted, invalid header sequence.")
                }
                .SelectMany(t => t.Item1.Select(headers => new[] { headers, t.Item2 }));
            }
        }
    }
}
