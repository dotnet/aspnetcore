// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpOverrides;

namespace Microsoft.AspNet.Builder
{
    public class OverrideHeaderOptions
    {
        public ForwardedHeaders ForwardedOptions { get; set; }
    }
}
