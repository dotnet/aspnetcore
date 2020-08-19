// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Use to configure optional properties for creating a <see cref="RazorSourceDocument"/>.
    /// </summary>
    public sealed class RazorSourceDocumentProperties
    {
        /// <summary>
        /// A <see cref="RazorSourceDocumentProperties"/> with default values.
        /// </summary>
        internal static readonly RazorSourceDocumentProperties Default = new RazorSourceDocumentProperties();

        /// <summary>
        /// Creates a new <see cref="RazorSourceDocumentProperties"/>.
        /// </summary>
        public RazorSourceDocumentProperties()
        {
        }

        /// <summary>
        /// Creates a new <see cref="RazorSourceDocumentProperties"/>.
        /// </summary>
        /// <param name="filePath">
        /// The path to the source file. Provide an rooted path if possible. May be <c>null</c>.
        /// </param>
        /// <param name="relativePath">
        /// The project-relative path to the source file. May be <c>null</c>. Must be a non-rooted path.
        /// </param>
        public RazorSourceDocumentProperties(string filePath, string relativePath)
        {
            // We don't do any magic or validation here since we don't need to do any I/O or interation
            // with the file system. We didn't validate anything in 2.0 so we don't want any compat risk.
            FilePath = filePath;
            RelativePath = relativePath;
        }

        /// <summary>
        /// Gets the path to the source file. May be an absolute or project-relative path. May be <c>null</c>.
        /// </summary>
        /// <remarks>
        /// An absolute path must be provided to generate debuggable assemblies.
        /// </remarks>
        public string FilePath { get; }

        /// <summary>
        /// Gets the project-relative path to the source file. May be <c>null</c>.
        /// </summary>
        /// <remarks>
        /// The relative path (if provided) is used for display (error messages). The project-relative path may also
        /// be used to embed checksums of the original source documents to support runtime recompilation of Razor code.
        /// </remarks>
        public string RelativePath { get; }
    }
}
