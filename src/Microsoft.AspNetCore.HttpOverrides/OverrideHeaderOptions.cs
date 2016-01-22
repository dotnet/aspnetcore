// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.HttpOverrides;

namespace Microsoft.AspNetCore.Builder
{
    public class OverrideHeaderOptions
    {
        public ForwardedHeaders ForwardedOptions { get; set; }
    }
}
