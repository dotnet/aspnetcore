// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.TestObjects
{
    public class UpperCaseParameterTransform : IOutboundParameterTransformer
    {
        public string TransformOutbound(object value)
        {
            return value?.ToString()?.ToUpperInvariant();
        }
    }
}
