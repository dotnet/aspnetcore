// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.RenderTree;

[Flags]
internal enum InputAttributeFlags : byte
{
    None = 0,
    BooleanValue = 1,
    OmittedBooleanValue = 2,
    OmittedChecked = 4,
    BooleanBinding = 8,
}
