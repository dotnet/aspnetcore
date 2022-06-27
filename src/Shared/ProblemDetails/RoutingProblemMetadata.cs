// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

using Microsoft.AspNetCore.Http;

internal sealed class RoutingProblemMetadata : IProblemDetailsMetadata
{
    public RoutingProblemMetadata(int statusCode = StatusCodes.Status404NotFound)
    {
       StatusCode = statusCode;
    }

    public int? StatusCode { get; }

    public ProblemDetailsTypes ProblemType => ProblemDetailsTypes.Routing;
}
