// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class CircuitRootComponentOperation(RootComponentOperation operation, WebRootComponentDescriptor? descriptor = null)
{
    public RootComponentOperationType Type => operation.Type;

    public int SsrComponentId => operation.SsrComponentId;

    public WebRootComponentDescriptor? Descriptor => descriptor;
}
