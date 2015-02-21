// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An entry in the cache used by <see cref="RazorPreCompiler"/>.
    /// </summary>
    public class PrecompilationCacheEntry
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PrecompilationCacheEntry"/> for a successful parse.
        /// </summary>
        /// <param name="fileInfo">The <see cref="RazorFileInfo"/> of the file being cached.</param>
        /// <param name="syntaxTree">The <see cref="CodeAnalysis.SyntaxTree"/> to cache.</param>
        public PrecompilationCacheEntry([NotNull] RazorFileInfo fileInfo,
                                        [NotNull] SyntaxTree syntaxTree)
        {
            FileInfo = fileInfo;
            SyntaxTree = syntaxTree;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="PrecompilationCacheEntry"/> for a failed parse.
        /// </summary>
        /// <param name="diagnostics">The <see cref="IReadOnlyList{Diagnostic}"/> produced from parsing the Razor
        /// file. This does not contain <see cref="Diagnostic"/>s produced from compiling the parsed
        /// <see cref="CodeAnalysis.SyntaxTree"/>.</param>
        public PrecompilationCacheEntry([NotNull] IReadOnlyList<Diagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
        }

        /// <summary>
        /// Gets the <see cref="RazorFileInfo"/> associated with this cache entry instance.
        /// </summary>
        /// <remarks>
        /// This property is not <c>null</c> if <see cref="Success"/> is <c>true</c>.
        /// </remarks>
        public RazorFileInfo FileInfo { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxTree"/> produced from parsing the Razor file.
        /// </summary>
        /// <remarks>
        /// This property is not <c>null</c> if <see cref="Success"/> is <c>true</c>.
        /// </remarks>
        public SyntaxTree SyntaxTree { get; }

        /// <summary>
        /// Gets the <see cref="Diagnostic"/>s produced from parsing the generated contents of the file
        /// specified by <see cref="FileInfo"/>. This does not contain <see cref="Diagnostic"/>s produced from
        /// compiling the parsed <see cref="CodeAnalysis.SyntaxTree"/>.
        /// </summary>
        /// <remarks>
        /// This property is <c>null</c> if <see cref="Success"/> is <c>true</c>.
        /// </remarks>
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets a value that indicates if parsing was successful.
        /// </summary>
        public bool Success
        {
            get { return SyntaxTree != null; }
        }
    }
}