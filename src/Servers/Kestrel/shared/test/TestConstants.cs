// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.InternalTesting;

public class TestConstants
{
    public const int EOF = -4095;
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
}
