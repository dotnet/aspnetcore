// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Blazor.Layouts
{
    /// <summary>
    /// Indicates that the associated component type uses a specified layout.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class LayoutAttribute : Attribute
    {
        /// <summary>
        /// The type of the layout. The type always implements <see cref="ILayoutComponent"/>.
        /// </summary>
        public Type LayoutType { get; private set; }

        /// <summary>
        /// Constructs an instance of <see cref="LayoutAttribute"/>.
        /// </summary>
        /// <param name="layoutType">The type of the layout. This must implement <see cref="ILayoutComponent"/>.</param>
        public LayoutAttribute(Type layoutType)
        {
            LayoutType = layoutType ?? throw new ArgumentNullException(nameof(layoutType));

            if (!typeof(ILayoutComponent).IsAssignableFrom(layoutType))
            {
                throw new ArgumentException($"Invalid layout type: {layoutType.FullName} " +
                    $"does not implement {typeof(ILayoutComponent).FullName}.");
            }
        }
    }
}
