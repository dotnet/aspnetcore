// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Http.Core
{
    public class HttpResponseFeature : IHttpResponseFeature
    {
	    public HttpResponseFeature()
	    {
            StatusCode = 200;
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Body = Stream.Null;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public Stream Body { get; set; }

        public bool HeadersSent
        {
            get { return false; }
        }

        public void OnSendingHeaders(Action<object> callback, object state)
        {
            throw new NotSupportedException();
        }
    }
}