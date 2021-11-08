// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.TestObjects;

public class UpperCaseParameterTransform : IOutboundParameterTransformer
{
    public string TransformOutbound(object value)
    {
        return value?.ToString()?.ToUpperInvariant();
    }
}
