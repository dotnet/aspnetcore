// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

internal class ProblemMetadata : IProblemDetailsMetadata
{
    public ProblemMetadata(int? statusCode = null, ProblemDetailsTypes problemType = ProblemDetailsTypes.All)
    {
        ProblemType = problemType;
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }

    public ProblemDetailsTypes ProblemType { get; }
}
