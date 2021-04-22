// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Provides a <see cref="IViewCompiler"/>.
    /// </summary>
    public interface IViewCompilerProvider
    {
        /// <summary>
        /// Gets a <see cref="IViewCompiler"/>.
        /// </summary>
        /// <returns>The view compiler.</returns>
        IViewCompiler GetCompiler();
    }
}
