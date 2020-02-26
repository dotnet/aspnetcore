// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnProperties
    {
        [HasAttribute_ReturnsTrueForAttributesOnPropertiesAttribute]
        public string SomeProperty { get; set; }
    }
}
