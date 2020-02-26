// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnMethodsTest
    {
        [HasAttribute_ReturnsTrueForAttributesOnMethodsAttribute]
        public void SomeMethod() { }
    }
}
