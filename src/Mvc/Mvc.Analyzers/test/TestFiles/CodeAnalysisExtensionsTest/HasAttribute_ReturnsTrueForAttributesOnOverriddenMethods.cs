// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
