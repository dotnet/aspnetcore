// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Resources = Microsoft.AspNetCore.Mvc.DataAnnotations.Test.Resources;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

// Wrap resources to make them available as public properties for [Display]. That attribute does not support
// internal properties.
public static class TestResources
{
    public static string Type_Three_Name => "type three name " + CultureInfo.CurrentCulture;

    public static string DisplayAttribute_Description => Resources.DisplayAttribute_Description;

    public static string DisplayAttribute_Name => Resources.DisplayAttribute_Name;

    public static string DisplayAttribute_Prompt => Resources.DisplayAttribute_Prompt;

    public static string DisplayAttribute_CultureSensitiveName =>
        Resources.DisplayAttribute_Name + CultureInfo.CurrentUICulture;

    public static string DisplayAttribute_CultureSensitiveDescription =>
        Resources.DisplayAttribute_Description + CultureInfo.CurrentUICulture;
}
