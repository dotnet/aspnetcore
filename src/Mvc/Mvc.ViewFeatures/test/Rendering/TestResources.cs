// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Resources = Microsoft.AspNetCore.Mvc.ViewFeatures.Test.Resources;

namespace Microsoft.AspNetCore.Mvc
{
    // Wrap resources to make them available as public properties for [Display]. That attribute does not support
    // internal properties.
    public class TestResources
    {
        public static string DisplayAttribute_Name { get; } = Resources.DisplayAttribute_Name;
    }
}