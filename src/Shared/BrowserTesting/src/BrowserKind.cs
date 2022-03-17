// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.BrowserTesting;

[Flags]
public enum BrowserKind
{
    Chromium = 1,
    Firefox = 2,
    Webkit = 4
}
