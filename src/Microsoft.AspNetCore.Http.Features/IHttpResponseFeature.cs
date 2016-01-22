// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public interface IHttpResponseFeature
    {
        int StatusCode { get; set; }
        string ReasonPhrase { get; set; }
        IHeaderDictionary Headers { get; set; }
        Stream Body { get; set; }
        bool HasStarted { get; }
        void OnStarting(Func<object, Task> callback, object state);
        void OnCompleted(Func<object, Task> callback, object state);
    }
}
