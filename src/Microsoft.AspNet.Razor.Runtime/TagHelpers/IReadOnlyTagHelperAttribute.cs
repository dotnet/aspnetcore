// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// A read-only HTML tag helper attribute.
    /// </summary>
    public interface IReadOnlyTagHelperAttribute : IEquatable<IReadOnlyTagHelperAttribute>
    {
        /// <summary>
        /// Gets the name of the attribute.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value of the attribute.
        /// </summary>
        object Value { get; }

        /// <summary>
        /// Gets an indication whether the attribute is minimized or not.
        /// </summary>
        /// <remarks>If <c>true</c>, <see cref="Value"/> will be ignored.</remarks>
        bool Minimized { get; }
    }
}