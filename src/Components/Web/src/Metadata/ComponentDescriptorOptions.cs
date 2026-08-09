// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

internal sealed class ComponentDescriptorOptions
{
    public IList<ComponentDescriptor> Components { get; } = [];
}
