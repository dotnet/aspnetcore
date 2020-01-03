// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Specifies that the attributed property should be bound using request services.
    /// <para>
    /// This attribute is used as the backing attribute for the <c>@inject</c>
    /// Razor directive.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RazorInjectAttribute : Attribute
    {
    }
}
