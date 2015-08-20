// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Features.Internal
{
    public class HttpResponseFeature : IHttpResponseFeature
    {
        public HttpResponseFeature()
        {
            StatusCode = 200;
            Headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            Body = Stream.Null;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, StringValues> Headers { get; set; }

        public Stream Body { get; set; }

        public bool HasStarted
        {
            get { return false; }
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}
