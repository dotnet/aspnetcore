// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class RootComponentOperationBatch
{
    public long BatchId { get; set; }

    public required RootComponentOperation[] Operations { get; set; }

    private string GetDebuggerDisplay()
    {
        return $"{nameof(RootComponentOperationBatch)}: {BatchId}, Operations Count: {Operations.Length}";
    }
}
