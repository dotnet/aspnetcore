// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc;

internal sealed class ProblemDetailsApiMapPolicy : IProblemDetailsMapPolicy
{
    private readonly ApiBehaviorOptions _options;

    public ProblemDetailsApiMapPolicy(IOptions<ApiBehaviorOptions> options)
    {
        _options = options.Value;
    }

    public bool CanMap(HttpContext context, EndpointMetadataCollection? metadata, int? statusCode, bool isRouting)
    {
        if (metadata != null)
        {
            // It is a Controller but not declared as ApiController behavior
            // or the SuppressMapClientErrors is true. In this case we will
            // not allow ProblemDetails mapping
            if (metadata.GetMetadata<ControllerAttribute>() != null)
            {
                return !(metadata.GetMetadata<ApiControllerAttribute>() == null ||
                    _options.SuppressMapClientErrors);
            }
        }

        return true;
    }
}
