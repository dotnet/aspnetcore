// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.TestHost
{
    internal class ResponseInformation : IHttpResponseFeature
    {
        public ResponseInformation()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Body = new MemoryStream();
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public Stream Body { get; set; }

        public void OnSendingHeaders(Action<object> callback, object state)
        {
            // TODO: Figure out how to implement this thing.
        }
    }
}
