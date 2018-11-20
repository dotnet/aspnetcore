// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public class HttpResponseFeature : IHttpResponseFeature
    {
        public HttpResponseFeature()
        {
            StatusCode = 200;
            Headers = new HeaderDictionary();
            Body = Stream.Null;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

        public virtual bool HasStarted
        {
            get { return false; }
        }

        public virtual void OnStarting(Func<object, Task> callback, object state)
        {
        }

        public virtual void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
