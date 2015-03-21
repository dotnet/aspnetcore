// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    // Wrap resources to make them available as public properties for [Display]. That attribute does not support
    // internal properties.
    public static class TestResources
    {
        public static string DisplayAttribute_Description { get; } = Resources.DisplayAttribute_Description;

        public static string DisplayAttribute_Name { get; } = Resources.DisplayAttribute_Name;
    }
}