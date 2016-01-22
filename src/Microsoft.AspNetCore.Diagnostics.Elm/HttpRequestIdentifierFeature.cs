// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.Diagnostics.Elm
{
    internal class HttpRequestIdentifierFeature : IHttpRequestIdentifierFeature
    {
        public string TraceIdentifier { get; set; }
    }
}