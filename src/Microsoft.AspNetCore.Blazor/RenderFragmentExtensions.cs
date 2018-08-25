// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor
{
    /// <summary>
    /// Contains extension methods for <see cref="RenderFragment"/> and <see cref="RenderFragment{T}"/>.
    /// </summary>
    public static class RenderFragmentExtensions
    {
        /// <summary>
        /// Binds a <see cref="RenderFragment{T}" /> and a value of type <typeparamref name="T"/> to a
        /// <see cref="RenderFragment"/> so that it can be used by the rendering system from Razor code.
        /// </summary>
        /// <typeparam name="T">The type of the value used by the <paramref name="fragment"/>.</typeparam>
        /// <param name="fragment">A <see cref="RenderFragment{T}"/>, usually produced by a Razor template.</param>
        /// <param name="value">The value of type <typeparamref name="T"/>.</param>
        /// <returns>A <see cref="RenderFragment"/>.</returns>
        public static RenderFragment WithValue<T>(this RenderFragment<T> fragment, T value)
        {
            return (b) => fragment(b, value);
        }
    }
}
