// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Test.Resources;

namespace Microsoft.AspNetCore.Mvc;

// Wrap resources to make them available as public properties for [Display]. That attribute does not support
// internal properties.
public class TestResources
{
    public static string DisplayAttribute_Name { get; } = Resources.DisplayAttribute_Name;
}
