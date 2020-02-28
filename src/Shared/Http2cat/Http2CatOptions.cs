// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http2Cat
{
    internal class Http2CatOptions
    {
        public string Url { get; set; }
        public Func<Http2Utilities, Task> Scenaro { get; set; }
    }
}
