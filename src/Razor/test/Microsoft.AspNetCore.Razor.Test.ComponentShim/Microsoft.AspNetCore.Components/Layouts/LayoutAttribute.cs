// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Layouts
{
    /// <summary>
    /// Indicates that the associated component type uses a specified layout.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class LayoutAttribute : Attribute
    {
        public LayoutAttribute(Type layoutType)
        {
            LayoutType = layoutType ?? throw new ArgumentNullException(nameof(layoutType));
        }

        public Type LayoutType { get; }
    }
}
