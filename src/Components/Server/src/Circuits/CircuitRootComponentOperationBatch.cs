// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class CircuitRootComponentOperationBatch(long batchId, CircuitRootComponentOperation[] operations)
{
    public long BatchId => batchId;

    public CircuitRootComponentOperation[] Operations => operations;
}
