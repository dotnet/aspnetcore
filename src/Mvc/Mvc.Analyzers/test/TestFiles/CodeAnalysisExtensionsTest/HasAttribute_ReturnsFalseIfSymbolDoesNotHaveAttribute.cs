// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
