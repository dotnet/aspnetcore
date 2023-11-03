// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

[Flags]
public enum OperatingSystems
{
    Linux = 1,
    MacOSX = 2,
    Windows = 4,
}
