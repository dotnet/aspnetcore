// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Indicates that the associated component type uses a specified layout.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class LayoutAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="LayoutAttribute"/>.
        /// </summary>
        /// <param name="layoutType">The type of the layout.</param>
        public LayoutAttribute(Type layoutType)
        {
            LayoutType = layoutType ?? throw new ArgumentNullException(nameof(layoutType));

            if (!typeof(IComponent).IsAssignableFrom(layoutType))
            {
                throw new ArgumentException($"Invalid layout type: {layoutType.FullName} " +
                    $"does not implement {typeof(IComponent).FullName}.");
            }

            // Note that we can't validate its acceptance of a 'Body' parameter at this stage,
            // because the contract doesn't force them to be known statically. However it will
            // be a runtime error if the referenced component type rejects the 'Body' parameter
            // when it gets used.
        }

        /// <summary>
        /// The type of the layout. The type must implement <see cref="IComponent"/>
        /// and must accept a parameter with the name 'Body'.
        /// </summary>
        public Type LayoutType { get; private set; }
    }
}
