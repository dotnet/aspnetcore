// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Indicates that the type and any derived types that this attribute is applied to
    /// is not considered a view component by the default view component discovery mechanism.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NonViewComponentAttribute : Attribute
    {
    }
}
