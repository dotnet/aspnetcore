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
    internal class RequestInformation : IHttpRequestFeature
    {
        public RequestInformation()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            PathBase = "";
            Body = Stream.Null;
            Protocol = "HTTP/1.1";
        }

        public Stream Body { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string PathBase { get; set; }

        public string Protocol { get; set; }

        public string QueryString { get; set; }

        public string Scheme { get; set; }
    }
}
