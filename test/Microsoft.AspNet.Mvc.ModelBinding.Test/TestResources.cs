// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    // This class is a 'fake' resources for testing DisplayAttribute. We can't use actual resources
    // because our generator makes it an internal class, which doesn't work with DisplayAttribute.
    public static class TestResources
    {
        public static string DisplayAttribute_Description { get; } = "description from resources";

        public static string DisplayAttribute_Name { get; } = "name from resources";
    }
}