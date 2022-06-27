// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

internal class ProblemMetadata : IProblemDetailsMetadata
{
    public ProblemMetadata(ProblemDetailsTypes problemType = ProblemDetailsTypes.All)
    {
        ProblemType = problemType;
    }

    public int? StatusCode => null;

    public ProblemDetailsTypes ProblemType { get; }
}
