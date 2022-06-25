// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

internal class ProblemMetadata : IProblemMetadata
{
    public ProblemMetadata(ProblemTypes problemType = ProblemTypes.All)
    {
        ProblemType = problemType;
    }

    public int? StatusCode => null;

    public ProblemTypes ProblemType { get; }
}
