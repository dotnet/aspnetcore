// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    /// <summary>
    /// An attribute for base classes for Pages and PageModels. Applying this attribute to a type
    /// suppresses discovery of handler methods and bound properties for that type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PagesBaseClassAttribute : Attribute
    {
    }
}
