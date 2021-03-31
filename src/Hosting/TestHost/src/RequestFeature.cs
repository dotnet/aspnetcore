// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class RequestFeature : IHttpRequestFeature
    {
        public RequestFeature()
        {
            Body = Stream.Null;
            Headers = new HeaderDictionary();
            Method = "GET";
            Path = "";
            PathBase = "";
            Protocol = HttpProtocol.Http11;
            QueryString = "";
            Scheme = "http";
            RawTarget = "";
        }

        public Stream Body { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string PathBase { get; set; }

        public string Protocol { get; set; }

        public string QueryString { get; set; }

        public string Scheme { get; set; }

        public string RawTarget { get; set; }
    }
}
