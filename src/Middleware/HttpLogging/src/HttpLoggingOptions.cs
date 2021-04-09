// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class HttpLoggingOptions
    {
        // TODO make these reloadable.
        public bool DisableRequestLogging { get; set; } = false;
        public bool DisableResponseLogging { get; set; } = false;

        public int RequestBodyLogLimit { get; set; } = 32 * 1024; // 32KB
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(1);

        public int ResponseBodyLogLimit { get; set; } = 32 * 1024; // 32KB

    }
}
