// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.HttpFeature
{
    [AssemblyNeutral]
    public interface IHttpResponseFeature
    {
        int StatusCode { get; set; }
        string ReasonPhrase { get; set; }
        IDictionary<string, string[]> Headers { get; set; }
        Stream Body { get; set; }
        bool HeadersSent { get; }
        void OnSendingHeaders(Action<object> callback, object state);
    }
}
