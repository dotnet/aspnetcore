// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationEntry
    {
        public string OutboundHeaderName { get; set; }
        public StringValues DefaultValues { get; set; }
        public Func<string, HttpContext, StringValues> ValueFactory { get; set; }
    }
}
