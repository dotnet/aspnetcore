// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Builder.Extensions
{
    public class FakeHttpRequestFeature : IHttpRequestFeature
    {
        public string Protocol { get; set; }
        public string Scheme { get; set; }
        public string Method { get; set; }
        public string PathBase { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public IDictionary<string, string[]> Headers { get; set; }
        public Stream Body { get; set; }
    }

    public class FakeHttpResponseFeature : IHttpResponseFeature
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public IDictionary<string, string[]> Headers { get; set; }
        public Stream Body { get; set; }
        public void OnSendingHeaders(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}