// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost;

internal sealed class RequestBodyDetectionFeature : IHttpRequestBodyDetectionFeature
{
    public RequestBodyDetectionFeature(bool canHaveBody)
    {
        CanHaveBody = canHaveBody;
    }

    public bool CanHaveBody { get; }
}
