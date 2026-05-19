// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp;

// Wrap resources to make them available as public properties for [Display]. That attribute does not support
// internal properties.
public static class TestResources
{
    public static string ProductName => Resources.ProductName;
}
