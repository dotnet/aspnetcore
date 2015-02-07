// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Mvc.Razor.Directives
{
    /// <summary>
    /// A cache for parsed <see cref="CodeTree"/>s.
    /// </summary>
    public interface ICodeTreeCache
    {
        /// <summary>
        /// Get an existing <see cref="CodeTree"/>, or create and add a new one if it is
        /// not available in the cache or is expired.
        /// </summary>
        /// <param name="pagePath">The application relative path of the Razor page.</param>
        /// <param name="getCodeTree">A delegate that creates a new <see cref="CodeTree"/>.</param>
        /// <returns>The <see cref="CodeTree"/> if a file exists at <paramref name="pagePath"/>,
        /// <c>null</c> otherwise.</returns>
        /// <remarks>The resulting <see cref="CodeTree"/> does not contain inherited chunks from _ViewStart or
        /// default inherited chunks.</remarks>
        CodeTree GetOrAdd(string pagePath, Func<IFileInfo, CodeTree> getCodeTree);
    }
}