// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class FakeOptions
    {
        public bool Configured { get; set; }
        public string Environment { get; set; }
        public string Message { get; set; }
    }
}