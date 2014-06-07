// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpFeature;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for CallContext
    /// </summary>
    public class CallContext :
        IHttpRequestFeature,
        IHttpResponseFeature
    {
        public CallContext()
        {
            ((IHttpResponseFeature)this).StatusCode = 200;
        }

        Stream IHttpResponseFeature.Body { get; set; }

        Stream IHttpRequestFeature.Body { get; set; }

        IDictionary<string, string[]> IHttpResponseFeature.Headers { get; set; }

        IDictionary<string, string[]> IHttpRequestFeature.Headers { get; set; }

        string IHttpRequestFeature.Method { get; set; }

        string IHttpRequestFeature.Path { get; set; }

        string IHttpRequestFeature.PathBase { get; set; }

        string IHttpRequestFeature.Protocol { get; set; }

        string IHttpRequestFeature.QueryString { get; set; }

        string IHttpResponseFeature.ReasonPhrase { get; set; }

        string IHttpRequestFeature.Scheme { get; set; }

        int IHttpResponseFeature.StatusCode { get; set; }

        void IHttpResponseFeature.OnSendingHeaders(Action<object> callback, object state)
        {
        }
    }
}