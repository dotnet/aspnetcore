// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class ReasonPhrases
    {
        private static readonly byte[] _bytesStatus100 = Encoding.ASCII.GetBytes("100 Continue");
        private static readonly byte[] _bytesStatus101 = Encoding.ASCII.GetBytes("101 Switching Protocols");
        private static readonly byte[] _bytesStatus102 = Encoding.ASCII.GetBytes("102 Processing");
        private static readonly byte[] _bytesStatus200 = Encoding.ASCII.GetBytes("200 OK");
        private static readonly byte[] _bytesStatus201 = Encoding.ASCII.GetBytes("201 Created");
        private static readonly byte[] _bytesStatus202 = Encoding.ASCII.GetBytes("202 Accepted");
        private static readonly byte[] _bytesStatus203 = Encoding.ASCII.GetBytes("203 Non-Authoritative Information");
        private static readonly byte[] _bytesStatus204 = Encoding.ASCII.GetBytes("204 No Content");
        private static readonly byte[] _bytesStatus205 = Encoding.ASCII.GetBytes("205 Reset Content");
        private static readonly byte[] _bytesStatus206 = Encoding.ASCII.GetBytes("206 Partial Content");
        private static readonly byte[] _bytesStatus207 = Encoding.ASCII.GetBytes("207 Multi-Status");
        private static readonly byte[] _bytesStatus226 = Encoding.ASCII.GetBytes("226 IM Used");
        private static readonly byte[] _bytesStatus300 = Encoding.ASCII.GetBytes("300 Multiple Choices");
        private static readonly byte[] _bytesStatus301 = Encoding.ASCII.GetBytes("301 Moved Permanently");
        private static readonly byte[] _bytesStatus302 = Encoding.ASCII.GetBytes("302 Found");
        private static readonly byte[] _bytesStatus303 = Encoding.ASCII.GetBytes("303 See Other");
        private static readonly byte[] _bytesStatus304 = Encoding.ASCII.GetBytes("304 Not Modified");
        private static readonly byte[] _bytesStatus305 = Encoding.ASCII.GetBytes("305 Use Proxy");
        private static readonly byte[] _bytesStatus306 = Encoding.ASCII.GetBytes("306 Reserved");
        private static readonly byte[] _bytesStatus307 = Encoding.ASCII.GetBytes("307 Temporary Redirect");
        private static readonly byte[] _bytesStatus400 = Encoding.ASCII.GetBytes("400 Bad Request");
        private static readonly byte[] _bytesStatus401 = Encoding.ASCII.GetBytes("401 Unauthorized");
        private static readonly byte[] _bytesStatus402 = Encoding.ASCII.GetBytes("402 Payment Required");
        private static readonly byte[] _bytesStatus403 = Encoding.ASCII.GetBytes("403 Forbidden");
        private static readonly byte[] _bytesStatus404 = Encoding.ASCII.GetBytes("404 Not Found");
        private static readonly byte[] _bytesStatus405 = Encoding.ASCII.GetBytes("405 Method Not Allowed");
        private static readonly byte[] _bytesStatus406 = Encoding.ASCII.GetBytes("406 Not Acceptable");
        private static readonly byte[] _bytesStatus407 = Encoding.ASCII.GetBytes("407 Proxy Authentication Required");
        private static readonly byte[] _bytesStatus408 = Encoding.ASCII.GetBytes("408 Request Timeout");
        private static readonly byte[] _bytesStatus409 = Encoding.ASCII.GetBytes("409 Conflict");
        private static readonly byte[] _bytesStatus410 = Encoding.ASCII.GetBytes("410 Gone");
        private static readonly byte[] _bytesStatus411 = Encoding.ASCII.GetBytes("411 Length Required");
        private static readonly byte[] _bytesStatus412 = Encoding.ASCII.GetBytes("412 Precondition Failed");
        private static readonly byte[] _bytesStatus413 = Encoding.ASCII.GetBytes("413 Payload Too Large");
        private static readonly byte[] _bytesStatus414 = Encoding.ASCII.GetBytes("414 URI Too Long");
        private static readonly byte[] _bytesStatus415 = Encoding.ASCII.GetBytes("415 Unsupported Media Type");
        private static readonly byte[] _bytesStatus416 = Encoding.ASCII.GetBytes("416 Range Not Satisfiable");
        private static readonly byte[] _bytesStatus417 = Encoding.ASCII.GetBytes("417 Expectation Failed");
        private static readonly byte[] _bytesStatus418 = Encoding.ASCII.GetBytes("418 I'm a Teapot");
        private static readonly byte[] _bytesStatus422 = Encoding.ASCII.GetBytes("422 Unprocessable Entity");
        private static readonly byte[] _bytesStatus423 = Encoding.ASCII.GetBytes("423 Locked");
        private static readonly byte[] _bytesStatus424 = Encoding.ASCII.GetBytes("424 Failed Dependency");
        private static readonly byte[] _bytesStatus426 = Encoding.ASCII.GetBytes("426 Upgrade Required");
        private static readonly byte[] _bytesStatus500 = Encoding.ASCII.GetBytes("500 Internal Server Error");
        private static readonly byte[] _bytesStatus501 = Encoding.ASCII.GetBytes("501 Not Implemented");
        private static readonly byte[] _bytesStatus502 = Encoding.ASCII.GetBytes("502 Bad Gateway");
        private static readonly byte[] _bytesStatus503 = Encoding.ASCII.GetBytes("503 Service Unavailable");
        private static readonly byte[] _bytesStatus504 = Encoding.ASCII.GetBytes("504 Gateway Timeout");
        private static readonly byte[] _bytesStatus505 = Encoding.ASCII.GetBytes("505 HTTP Version Not Supported");
        private static readonly byte[] _bytesStatus506 = Encoding.ASCII.GetBytes("506 Variant Also Negotiates");
        private static readonly byte[] _bytesStatus507 = Encoding.ASCII.GetBytes("507 Insufficient Storage");
        private static readonly byte[] _bytesStatus510 = Encoding.ASCII.GetBytes("510 Not Extended");

        public static byte[] ToStatusBytes(int statusCode, string reasonPhrase = null)
        {
            if (string.IsNullOrEmpty(reasonPhrase))
            {
                switch (statusCode)
                {
                    case 100:
                        return _bytesStatus100;
                    case 101:
                        return _bytesStatus101;
                    case 102:
                        return _bytesStatus102;
                    case 200:
                        return _bytesStatus200;
                    case 201:
                        return _bytesStatus201;
                    case 202:
                        return _bytesStatus202;
                    case 203:
                        return _bytesStatus203;
                    case 204:
                        return _bytesStatus204;
                    case 205:
                        return _bytesStatus205;
                    case 206:
                        return _bytesStatus206;
                    case 207:
                        return _bytesStatus207;
                    case 226:
                        return _bytesStatus226;
                    case 300:
                        return _bytesStatus300;
                    case 301:
                        return _bytesStatus301;
                    case 302:
                        return _bytesStatus302;
                    case 303:
                        return _bytesStatus303;
                    case 304:
                        return _bytesStatus304;
                    case 305:
                        return _bytesStatus305;
                    case 306:
                        return _bytesStatus306;
                    case 307:
                        return _bytesStatus307;
                    case 400:
                        return _bytesStatus400;
                    case 401:
                        return _bytesStatus401;
                    case 402:
                        return _bytesStatus402;
                    case 403:
                        return _bytesStatus403;
                    case 404:
                        return _bytesStatus404;
                    case 405:
                        return _bytesStatus405;
                    case 406:
                        return _bytesStatus406;
                    case 407:
                        return _bytesStatus407;
                    case 408:
                        return _bytesStatus408;
                    case 409:
                        return _bytesStatus409;
                    case 410:
                        return _bytesStatus410;
                    case 411:
                        return _bytesStatus411;
                    case 412:
                        return _bytesStatus412;
                    case 413:
                        return _bytesStatus413;
                    case 414:
                        return _bytesStatus414;
                    case 415:
                        return _bytesStatus415;
                    case 416:
                        return _bytesStatus416;
                    case 417:
                        return _bytesStatus417;
                    case 418:
                        return _bytesStatus418;
                    case 422:
                        return _bytesStatus422;
                    case 423:
                        return _bytesStatus423;
                    case 424:
                        return _bytesStatus424;
                    case 426:
                        return _bytesStatus426;
                    case 500:
                        return _bytesStatus500;
                    case 501:
                        return _bytesStatus501;
                    case 502:
                        return _bytesStatus502;
                    case 503:
                        return _bytesStatus503;
                    case 504:
                        return _bytesStatus504;
                    case 505:
                        return _bytesStatus505;
                    case 506:
                        return _bytesStatus506;
                    case 507:
                        return _bytesStatus507;
                    case 510:
                        return _bytesStatus510;
                    default:
                        return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture) + " Unknown");
                }
            }
            return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase);
        }
    }
}