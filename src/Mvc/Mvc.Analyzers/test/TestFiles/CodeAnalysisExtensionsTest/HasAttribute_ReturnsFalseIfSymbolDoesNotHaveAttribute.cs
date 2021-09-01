// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttribute : Attribute { }

    [Controller]
    public class HasAttribute_ReturnsFalseIfTypeDoesNotHaveAttributeTest
    {
        [NonAction]
        public void SomeMethod()
        {

        }

        [BindProperty]
        public string SomeProperty { get; set; }
    }
}
