// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Features
{
    public interface IHttpRequestFeature
    {
        string Protocol { get; set; }
        string Scheme { get; set; }
        string Method { get; set; }
        string PathBase { get; set; }
        string Path { get; set; }
        string QueryString { get; set; }
        IDictionary<string, StringValues> Headers { get; set; }
        Stream Body { get; set; }
    }
}
