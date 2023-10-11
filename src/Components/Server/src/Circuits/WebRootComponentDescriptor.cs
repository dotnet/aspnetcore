// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class WebRootComponentDescriptor(
    Type componentType,
    ComponentMarkerKey? key,
    WebRootComponentParameters parameters)
{
    public Type ComponentType => componentType;

    public ComponentMarkerKey? Key => key;

    public WebRootComponentParameters Parameters => parameters;
}
