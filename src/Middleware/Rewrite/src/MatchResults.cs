// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Rewrite;

internal sealed class MatchResults
{
    public static readonly MatchResults EmptySuccess = new MatchResults(success: true);
    public static readonly MatchResults EmptyFailure = new MatchResults(success: false);

    public MatchResults(bool success, BackReferenceCollection? backReferences = null)
    {
        Success = success;
        BackReferences = backReferences;
    }

    [MemberNotNullWhen(true, nameof(BackReferences))]
    public bool Success { get; }
    public BackReferenceCollection? BackReferences { get; }
}
