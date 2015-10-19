// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Contains property metadata.
    /// </summary>
    public interface IPropertyInfo : IMemberInfo
    {
        /// <summary>
        /// Gets a value indicating whether this property has a public getter.
        /// </summary>
        bool HasPublicGetter { get; }

        /// <summary>
        /// Gets a value indicating whether this property has a public setter.
        /// </summary>
        bool HasPublicSetter { get; }

        /// <summary>
        /// Gets the <see cref="ITypeInfo"/> of the property.
        /// </summary>
        ITypeInfo PropertyType { get; }
    }
}