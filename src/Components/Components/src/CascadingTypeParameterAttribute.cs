// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Denotes the generic type parameter as cascading. This allows generic type inference
    /// to use this type parameter value automatically on descendants that also have a type
    /// parameter with the same name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class CascadingTypeParameterAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of <see cref="CascadingTypeParameterAttribute"/>.
        /// </summary>
        /// <param name="name">The name of the type parameter.</param>
        public CascadingTypeParameterAttribute(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// Gets the name of the type parameter.
        /// </summary>
        public string Name { get; }
    }
}
