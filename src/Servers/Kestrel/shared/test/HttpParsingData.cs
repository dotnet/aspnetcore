// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class HttpParsingData
{
    public static IEnumerable<string[]> RequestLineValidData
    {
        get
        {
            var methods = new[]
            {
                    "GET",
                    "CUSTOM",
                };
            var paths = new[]
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
                    Tuple.Create("/%25%30%30", "/%00"),
                    Tuple.Create("/%%2000", "/% 00"),
                    Tuple.Create("/%2F", "/%2F"),
                    Tuple.Create("http://host/abs/path", "/abs/path"),
                    Tuple.Create("http://host/abs/path/", "/abs/path/"),
                    Tuple.Create("http://host/a%20b%20c/", "/a b c/"),
                    Tuple.Create("https://host/abs/path", "/abs/path"),
                    Tuple.Create("https://host/abs/path/", "/abs/path/"),
                    Tuple.Create("https://host:22/abs/path", "/abs/path"),
                    Tuple.Create("https://user@host:9080/abs/path", "/abs/path"),
                    Tuple.Create("http://host/", "/"),
                    Tuple.Create("http://host", "/"),
                    Tuple.Create("https://host/", "/"),
                    Tuple.Create("https://host", "/"),
                    Tuple.Create("http://user@host/", "/"),
                    Tuple.Create("http://127.0.0.1/", "/"),
                    Tuple.Create("http://user@127.0.0.1/", "/"),
                    Tuple.Create("http://user@127.0.0.1:8080/", "/"),
                    Tuple.Create("http://127.0.0.1:8080/", "/"),
                    Tuple.Create("http://[::1]", "/"),
                    Tuple.Create("http://[::1]/path", "/path"),
                    Tuple.Create("http://[::1]:8080/", "/"),
                    Tuple.Create("http://user@[::1]:8080/", "/"),
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
                    "HTTP/1.1",
                    " HTTP/1.1",
                    "   HTTP/1.1"
                };

            return from method in methods
                   from path in paths
                   from queryString in queryStrings
                   from httpVersion in httpVersions
                   select new[]
                   {
                           $"{method} {path.Item1}{queryString} {httpVersion}\r\n",
                           method,
                           $"{path.Item1}{queryString}",
                           $"{path.Item1}",
                           $"{path.Item2}",
                           queryString,
                           httpVersion.Trim()
                       };
        }
    }

    public static IEnumerable<string[]> RequestLineDotSegmentData => new[]
    {
            new[] { "GET /a/../b HTTP/1.1\r\n", "/a/../b", "/b", "" },
            new[] { "GET /%61/../%62 HTTP/1.1\r\n", "/%61/../%62", "/b", "" },
            new[] { "GET /a/%2E%2E/b HTTP/1.1\r\n", "/a/%2E%2E/b", "/b", "" },
            new[] { "GET /%61/%2E%2E/%62 HTTP/1.1\r\n", "/%61/%2E%2E/%62", "/b", "" },
            new[] { "GET /a?p=/a/../b HTTP/1.1\r\n", "/a?p=/a/../b", "/a", "?p=/a/../b" },
            new[] { "GET /a?p=/a/%2E%2E/b HTTP/1.1\r\n", "/a?p=/a/%2E%2E/b", "/a", "?p=/a/%2E%2E/b" },
            new[] { "GET http://example.com/a/../b HTTP/1.1\r\n", "http://example.com/a/../b", "/b", "" },
            new[] { "GET http://example.com/%61/../%62 HTTP/1.1\r\n", "http://example.com/%61/../%62", "/b", "" },
            new[] { "GET http://example.com/a/%2E%2E/b HTTP/1.1\r\n", "http://example.com/a/%2E%2E/b", "/b", "" },
            new[] { "GET http://example.com/%61/%2E%2E/%62 HTTP/1.1\r\n", "http://example.com/%61/%2E%2E/%62", "/b", "" },
            new[] { "GET http://example.com/a?p=/a/../b HTTP/1.1\r\n", "http://example.com/a?p=/a/../b", "/a", "?p=/a/../b" },
            new[] { "GET http://example.com/a?p=/a/%2E%2E/b HTTP/1.1\r\n", "http://example.com/a?p=/a/%2E%2E/b", "/a", "?p=/a/%2E%2E/b" },
            new[] { "GET http://example.com?p=/a/../b HTTP/1.1\r\n", "http://example.com?p=/a/../b", "/", "?p=/a/../b" },
            new[] { "GET http://example.com?p=/a/%2E%2E/b HTTP/1.1\r\n", "http://example.com?p=/a/%2E%2E/b", "/", "?p=/a/%2E%2E/b" },

            // Asterisk-form and authority-form should be unaffected and cause no issues
            new[] { "OPTIONS * HTTP/1.1\r\n", "*", "", "" },
            new[] { "CONNECT www.example.com HTTP/1.1\r\n", "www.example.com", "", "" },
        };

    public static IEnumerable<string> RequestLineIncompleteData => new[]
    {
            "G",
            "GE",
            "GET",
            "GET ",
            "GET /",
            "GET / ",
            "GET / H",
            "GET / HT",
            "GET / HTT",
            "GET / HTTP",
            "GET / HTTP/",
            "GET / HTTP/1",
            "GET / HTTP/1.",
            "GET / HTTP/1.1",
            "GET / HTTP/1.1\r",
        };

    public static IEnumerable<string> RequestLineInvalidData
    {
        get
        {
            return new[]
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
                    "GET  / HTTP/1.1\r\n",
                    "GET   / HTTP/1.1\r\n",
                    "GET  /  HTTP/1.1\r\n",
                    "GET   /   HTTP/1.1\r\n",
                    "GET / HTTP/1.1 \r\n",
                    "GET / HTTP/1.1  \r\n",
                    "GET / H\r\n",
                    "GET / HT\r\n",
                    "GET / HTT\r\n",
                    "GET / HTTP\r\n",
                    "GET / HTTP/\r\n",
                    "GET / HTTP/1\r\n",
                    "GET / HTTP/1.\r\n",
                    "GET / HTTP/1.1a\n",
                    "GET / HTTP/1.1a\r\n",
                    "GET / HTTP/1.1ab\r\n",
                    "GET / hello\r\n",
                    "GET? / HTTP/1.1\r\n",
                    "GET ? HTTP/1.1\r\n",
                    "GET /a?b=cHTTP/1.1\r\n",
                    "GET /a%20bHTTP/1.1\r\n",
                    "GET /a%20b?c=dHTTP/1.1\r\n",
                    "GET %2F HTTP/1.1\r\n",
                    "GET %00 HTTP/1.1\r\n",
                    "GET /?d=Bad UrlToAccept HTTP/1.1\r\n",
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
                    "CUSTOM  / HTTP/1.1\r\n",
                    "CUSTOM   / HTTP/1.1\r\n",
                    "CUSTOM  /  HTTP/1.1\r\n",
                    "CUSTOM   /   HTTP/1.1\r\n",
                    "CUSTOM / HTTP/1.1 \r\n",
                    "CUSTOM / HTTP/1.1  \r\n",
                    "CUSTOM / H\r\n",
                    "CUSTOM / HT\r\n",
                    "CUSTOM / HTT\r\n",
                    "CUSTOM / HTTP\r\n",
                    "CUSTOM / HTTP/\r\n",
                    "CUSTOM / HTTP/1\r\n",
                    "CUSTOM / HTTP/1.\r\n",
                    "CUSTOM / HTTP/1.1a\n",
                    "CUSTOM / HTTP/1.1a\r\n",
                    "CUSTOM / HTTP/1.1ab\r\n",
                    "CUSTOM / H\n",
                    "CUSTOM / HT\n",
                    "CUSTOM / HTT\n",
                    "CUSTOM / HTTP\n",
                    "CUSTOM / HTTP/\n",
                    "CUSTOM / HTTP/1\n",
                    "CUSTOM / HTTP/1.\n",
                    "CUSTOM / hello\r\n",
                    "CUSTOM / hello\n",
                    "CUSTOM ? HTTP/1.1\r\n",
                    "CUSTOM /a?b=cHTTP/1.1\r\n",
                    "CUSTOM /a%20bHTTP/1.1\r\n",
                    "CUSTOM /a%20b?c=dHTTP/1.1\r\n",
                    "CUSTOM %2F HTTP/1.1\r\n",
                    "CUSTOM %00 HTTP/1.1\r\n",
                    "CUSTOM /?d=Bad UrlToAccept HTTP/1.1\r\n",
                }.Concat(MethodWithNonTokenCharData.Select(method => $"{method} / HTTP/1.0\r\n"));
        }
    }

    // This list is valid in quirk mode
    public static IEnumerable<string> RequestLineInvalidDataLineFeedTerminator
    {
        get
        {
            return new[]
            {
                    "GET / HTTP/1.0\n",
                    "GET / HTTP/1.1\n",
                    "CUSTOM / HTTP/1.0\n",
                    "CUSTOM / HTTP/1.1\n",
                };
        }
    }

    // Bad HTTP Methods (invalid according to RFC)
    public static IEnumerable<string> MethodWithNonTokenCharData
    {
        get
        {
            return new[]
            {
                    "(",
                    ")",
                    "<",
                    ">",
                    "@",
                    ",",
                    ";",
                    ":",
                    "\\",
                    "\"",
                    "/",
                    "[",
                    "]",
                    "?",
                    "=",
                    "{",
                    "}",
                    "get@",
                    "post=",
                    "[0x00]"
                }.Concat(MethodWithNullCharData);
        }
    }

    public static IEnumerable<string> MethodWithNullCharData => new[]
    {
            // Bad HTTP Methods (invalid according to RFC)
            "\0",
            "\0GET",
            "G\0T",
            "GET\0",
        };

    public static IEnumerable<string> TargetWithEncodedNullCharData => new[]
    {
            "/%00",
            "/%00%00",
            "/%E8%00%84",
            "/%E8%85%00",
            "/%F3%00%82%86",
            "/%F3%85%00%82",
            "/%F3%85%82%00",
        };

    public static TheoryData<string, string> TargetInvalidData
    {
        get
        {
            var data = new TheoryData<string, string>();

            // Invalid absolute-form
            data.Add("GET", "http://");
            data.Add("GET", "http:/");
            data.Add("GET", "https:/");
            data.Add("GET", "http:///");
            data.Add("GET", "https://");
            data.Add("GET", "http:////");
            data.Add("GET", "http://:80");
            data.Add("GET", "http://:80/abc");
            data.Add("GET", "http://user@");
            data.Add("GET", "http://user@/abc");
            data.Add("GET", "http://abc%20xyz/abc");
            data.Add("GET", "http://%20/abc?query=%0A");
            // Valid absolute-form but with unsupported schemes
            data.Add("GET", "otherscheme://host/");
            data.Add("GET", "ws://host/");
            data.Add("GET", "wss://host/");
            // Must only have one asterisk
            data.Add("OPTIONS", "**");
            // Relative form
            data.Add("GET", "../../");
            data.Add("GET", "..\\.");

            return data;
        }
    }

    public static TheoryData<string, int> MethodNotAllowedRequestLine
    {
        get
        {
            var methods = new[]
            {
                    "GET",
                    "PUT",
                    "DELETE",
                    "POST",
                    "HEAD",
                    "TRACE",
                    "PATCH",
                    "CONNECT",
                    "OPTIONS",
                    "CUSTOM",
                };

            var data = new TheoryData<string, int>();

            foreach (var method in methods.Except(new[] { "OPTIONS" }))
            {
                data.Add($"{method} * HTTP/1.1\r\n", (int)HttpMethod.Options);
            }

            foreach (var method in methods.Except(new[] { "CONNECT" }))
            {
                data.Add($"{method} www.example.com:80 HTTP/1.1\r\n", (int)HttpMethod.Connect);
            }

            return data;
        }
    }

    public static IEnumerable<string> TargetWithNullCharData
    {
        get
        {
            return new[]
            {
                    "\0",
                    "/\0",
                    "/\0\0",
                    "/%C8\0",
                }.Concat(QueryStringWithNullCharData);
        }
    }

    public static IEnumerable<string> QueryStringWithNullCharData => new[]
    {
            "/?\0=a",
            "/?a=\0",
        };

    public static TheoryData<string> UnrecognizedHttpVersionData => new TheoryData<string>
        {
            "http/1.0",
            "http/1.1",
            "HTTP/1.2",
            "HTTP/3.0",
            "8charact",
        };

    public static IEnumerable<object[]> RequestHeaderInvalidDataLineFeedTerminator => new[]
    {
            // Missing CR
            new[] { "Header: value\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header: value\x0A") },
            new[] { "Header-1: value1\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: value1\x0A") },
            new[] { "Header-1: value1\r\nHeader-2: value2\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2: value2\x0A") },

            // Empty header name
            new[] { ":a\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@":a\x0A") },
        };

    public static IEnumerable<object[]> RequestHeaderInvalidData => new[]
    {
            // Line folding
            new[] { "Header: line1\r\n line2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" line2\x0D\x0A") },
            new[] { "Header: line1\r\n\tline2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09line2\x0D\x0A") },
            new[] { "Header: line1\r\n  line2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"  line2\x0D\x0A") },
            new[] { "Header: line1\r\n \tline2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" \x09line2\x0D\x0A") },
            new[] { "Header: line1\r\n\t line2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09 line2\x0D\x0A") },
            new[] { "Header: line1\r\n\t\tline2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09\x09line2\x0D\x0A") },
            new[] { "Header: line1\r\n \t\t line2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" \x09\x09 line2\x0D\x0A") },
            new[] { "Header: line1\r\n \t \t line2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" \x09 \x09 line2\x0D\x0A") },
            new[] { "Header-1: multi\r\n line\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" line\x0D\x0A") },
            new[] { "Header-1: value1\r\nHeader-2: multi\r\n line\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" line\x0D\x0A") },
            new[] { "Header-1: value1\r\n Header-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" Header-2: value2\x0D\x0A") },
            new[] { "Header-1: value1\r\n\tHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09Header-2: value2\x0D\x0A") },

            // CR in value
            new[] { "Header-1: value1\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: value1\x0D\x0D") },
            new[] { "Header-1: val\rue1\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: val\x0Du") },
            new[] { "Header-1: value1\rHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: value1\x0DH") },
            new[] { "Header-1: value1\r\nHeader-2: value2\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2: value2\x0D\x0D") },
            new[] { "Header-1: value1\r\nHeader-2: v\ralue2\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2: v\x0Da") },
            new[] { "Header-1: Value__\rVector16________Vector32\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value__\x0DV") },
            new[] { "Header-1: Value___Vector16\r________Vector32\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16\x0D_") },
            new[] { "Header-1: Value___Vector16_______\rVector32\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16_______\x0DV") },
            new[] { "Header-1: Value___Vector16________Vector32\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16________Vector32\x0D\x0D") },
            new[] { "Header-1: Value___Vector16________Vector32_\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16________Vector32_\x0D\x0D") },
            new[] { "Header-1: Value___Vector16________Vector32Value___Vector16_______\rVector32\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16________Vector32Value___Vector16_______\x0DV") },
            new[] { "Header-1: Value___Vector16________Vector32Value___Vector16________Vector32\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16________Vector32Value___Vector16________Vector32\x0D\x0D") },
            new[] { "Header-1: Value___Vector16________Vector32Value___Vector16________Vector32_\r\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1: Value___Vector16________Vector32Value___Vector16________Vector32_\x0D\x0D") },

            // Missing colon
            new[] { "Header-1 value1\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1 value1\x0D\x0A") },
            new[] { "Header-1 value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-1 value1\x0D\x0A") },
            new[] { "Header-1: value1\r\nHeader-2 value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2 value2\x0D\x0A") },
            new[] { "HeaderValue1\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"HeaderValue1\x0D\x0A") },

            // Starting with whitespace
            new[] { " Header: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" Header: value\x0D\x0A") },
            new[] { "\tHeader: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09Header: value\x0D\x0A") },
            new[] { " Header-1: value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@" Header-1: value1\x0D\x0A") },
            new[] { "\tHeader-1: value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"\x09Header-1: value1\x0D\x0A") },

            // Whitespace in header name
            new[] { "Header : value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header : value\x0D\x0A") },
            new[] { "Header\t: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header\x09: value\x0D\x0A") },
            new[] { "Header\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header\x0D:") },
            new[] { "Header_\rVector16: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header_\x0DV") },
            new[] { "Header__Vector16\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16\x0D:") },
            new[] { "Header__Vector16_\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16_\x0D:") },
            new[] { "Header_\rVector16________Vector32: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header_\x0DV") },
            new[] { "Header__Vector16________Vector32\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16________Vector32\x0D:") },
            new[] { "Header__Vector16________Vector32_\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16________Vector32_\x0D:") },
            new[] { "Header__Vector16________Vector32Header_\rVector16________Vector32: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16________Vector32Header_\x0DV") },
            new[] { "Header__Vector16________Vector32Header__Vector16________Vector32\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16________Vector32Header__Vector16________Vector32\x0D:") },
            new[] { "Header__Vector16________Vector32Header__Vector16________Vector32_\r: value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header__Vector16________Vector32Header__Vector16________Vector32_\x0D:") },
            new[] { "Header 1: value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header 1: value1\x0D\x0A") },
            new[] { "Header 1 : value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header 1 : value1\x0D\x0A") },
            new[] { "Header 1\t: value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header 1\x09: value1\x0D\x0A") },
            new[] { "Header 1\r: value1\r\nHeader-2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header 1\x0D:") },
            new[] { "Header-1: value1\r\nHeader 2: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header 2: value2\x0D\x0A") },
            new[] { "Header-1: value1\r\nHeader-2 : value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2 : value2\x0D\x0A") },
            new[] { "Header-1: value1\r\nHeader-2\t: value2\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2\x09: value2\x0D\x0A") },

            // Headers not ending in CRLF line
            new[] { "Header-1: value1\r\nHeader-2: value2\r\n\r\r", CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF },
            new[] { "Header-1: value1\r\nHeader-2: value2\r\n\r ", CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF },
            new[] { "Header-1: value1\r\nHeader-2: value2\r\n\r \n", CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF },
            new[] { "Header-1: value1\r\nHeader-2\t: value2 \n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@"Header-2\x09: value2 \x0A") },

            // Empty header name
            new[] { ": value\r\n\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@": value\x0D\x0A") },
            new[] { ":a\r\n", CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(@":a\x0D\x0A") },
        };

    public static TheoryData<string, string> HostHeaderData
        => new TheoryData<string, string>
            {
                    { "OPTIONS *", "" },
                    { "GET /pub/WWW/", "" },
                    { "GET /pub/WWW/", "   " },
                    { "GET /pub/WWW/", "." },
                    { "GET /pub/WWW/", "www.example.org" },
                    { "GET http://localhost/", "localhost" },
                    { "GET http://localhost:80/", "localhost:80" },
                    { "GET http://localhost:80/", "localhost" },
                    { "GET https://localhost/", "localhost" },
                    { "GET https://localhost:443/", "localhost:443" },
                    { "GET https://localhost:443/", "localhost" },
                    { "CONNECT asp.net:80", "asp.net:80" },
                    { "CONNECT asp.net:443", "asp.net:443" },
                    { "CONNECT user-images.githubusercontent.com:443", "user-images.githubusercontent.com:443" },
            };

    public static TheoryData<string, string> HostHeaderInvalidData
    {
        get
        {
            // see https://tools.ietf.org/html/rfc7230#section-5.4
            var invalidHostValues = new[] {
                    "",
                    "   ",
                    "contoso.com:4000",
                    "contoso.com/",
                    "not-contoso.com",
                    "user@password:contoso.com",
                    "user@contoso.com",
                    "http://contoso.com/",
                    "http://contoso.com"
                };

            var data = new TheoryData<string, string>();

            foreach (var host in invalidHostValues)
            {
                // absolute form
                // expected: GET http://contoso.com/ => Host: contoso.com
                data.Add("GET http://contoso.com/", host);

                // authority-form
                // expected: CONNECT contoso.com => Host: contoso.com
                data.Add("CONNECT contoso.com", host);
            }

            // port mismatch when target contains default https port
            data.Add("GET https://contoso.com:443/", "contoso.com:5000");
            data.Add("CONNECT contoso.com:443", "contoso.com:5000");

            // port mismatch when target contains default http port
            data.Add("GET http://contoso.com:80/", "contoso.com:5000");

            return data;
        }
    }
}
