// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Test;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    // Wrap resources to make them available as public properties for [Display]. That attribute does not support
    // internal properties.
    public static class TestResources
    {
        public static string DisplayAttribute_Description { get; } = Resources.DisplayAttribute_Description;

        public static string DisplayAttribute_Name { get; } = Resources.DisplayAttribute_Name;

        public static string DisplayAttribute_CultureSensitiveName =>
            Resources.DisplayAttribute_Name + CultureInfo.CurrentUICulture;

        public static string DisplayAttribute_CultureSensitiveDescription =>
            Resources.DisplayAttribute_Description + CultureInfo.CurrentUICulture;
    }
}