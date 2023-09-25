// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Server;

internal sealed class WebRootComponentDescriptor(
    Type componentType,
    string key,
    WebRootComponentParameters parameters)
{
    public Type ComponentType => componentType;

    public string Key => key;

    public WebRootComponentParameters Parameters => parameters;
}
