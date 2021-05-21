// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Default implementation for <see cref="IHttpResponseFeature"/>.
    /// </summary>
    public class HttpResponseFeature : IHttpResponseFeature
    {
        /// <summary>
        /// Initializes a new instance of <see cref="HttpResponseFeature"/>.
        /// </summary>
        public HttpResponseFeature()
        {
            StatusCode = 200;
            Headers = new HeaderDictionary();
            Body = Stream.Null;
        }

        /// <inheritdoc />
        public int StatusCode { get; set; }

        /// <inheritdoc />
        public string? ReasonPhrase { get; set; }

        /// <inheritdoc />
        public IHeaderDictionary Headers { get; set; }

        /// <inheritdoc />
        public Stream Body { get; set; }

        /// <inheritdoc />
        public virtual bool HasStarted => false;

        /// <inheritdoc />
        public virtual void OnStarting(Func<object, Task> callback, object state)
        {
        }

        /// <inheritdoc />
        public virtual void OnCompleted(Func<object, Task> callback, object state)
        {
        }
    }
}
