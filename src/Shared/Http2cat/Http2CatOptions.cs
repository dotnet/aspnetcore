// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace http2cat
{
    public class Http2CatOptions
    {
        public string Url { get; set; }
        public Func<Http2Utilities, ILogger, Task> Scenaro { get; set; }
    }
}
