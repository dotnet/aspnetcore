// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

[Flags]
public enum RuntimeFrameworks
{
    None = 0,
    Mono = 1 << 0,
    CLR = 1 << 1,
    CoreCLR = 1 << 2
}
