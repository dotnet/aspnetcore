// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery
{
    public class TestOptionsManager : IOptions<AntiforgeryOptions>
    {
        public TestOptionsManager()
        {
        }

        public TestOptionsManager(AntiforgeryOptions options)
        {
            Value = options;
        }

        public AntiforgeryOptions Value { get; set; } = new AntiforgeryOptions();
    }
}
