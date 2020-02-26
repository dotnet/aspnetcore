// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute : Attribute { }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsBase
    {
        [HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsAttribute]
        public virtual void SomeMethod() { }
    }

    public class HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsTest : HasAttribute_ReturnsTrueForAttributesOnOverriddenMethodsBase
    {
        public override void SomeMethod() { }
    }
}
