// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Authentication;

namespace Microsoft.AspNet.Builder
{
    public class ClaimsTransformationOptions
    {
        public IClaimsTransformer Transformer { get; set; }
    }
}
