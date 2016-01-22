// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Specifies that a tag helper property should be set with the current
    /// <see cref="Rendering.ViewContext"/> when creating the tag helper. The property must have a
    /// public set method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ViewContextAttribute : Attribute
    {
    }
}
