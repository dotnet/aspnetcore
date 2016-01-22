// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    public class TestCorsOptions : IOptions<CorsOptions>
    {
        public CorsOptions Value { get; set; }
    }
}
