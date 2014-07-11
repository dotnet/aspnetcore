// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    public class OutputFormatterContext
    {
        public ObjectResult ObjectResult { get; set; }

        public Type DeclaredType { get; set; }

        public HttpContext HttpContext { get; set; }
    }
}
