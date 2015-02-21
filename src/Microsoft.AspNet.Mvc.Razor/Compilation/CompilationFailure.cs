// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation of <see cref="ICompilationFailure"/>.
    /// </summary>
    public class CompilationFailure : ICompilationFailure
    {
        /// <summary>Initializes a new instance of <see cref="CompilationFailure"/>.</summary>
        /// <param name="filePath">The path of the Razor source file that was compiled.</param>
        /// <param name="fileContent">The contents of the Razor source file.</param>
        /// <param name="compiledContent">The generated C# content that was compiled.</param>
        /// <param name="messages">A sequence of <see cref="ICompilationMessage"/> encountered
        /// during compilation.</param>
        public CompilationFailure(
                [NotNull] string filePath,
                [NotNull] string fileContent,
                [NotNull] string compiledContent,
                [NotNull] IEnumerable<ICompilationMessage> messages)
        {
            SourceFilePath = filePath;
            SourceFileContent = fileContent;
            Messages = messages;
            CompiledContent = compiledContent;
        }

        /// <summary>
        /// Gets the path of the Razor source file that produced the compilation failure.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Gets the content of the Razor source file.
        /// </summary>
        public string SourceFileContent { get; }

        /// <summary>
        /// Gets the generated C# content that was compiled.
        /// </summary>
        public string CompiledContent { get; }

        /// <summary>
        /// Gets a sequence of <see cref="ICompilationMessage"/> instances encountered during compilation.
        /// </summary>
        public IEnumerable<ICompilationMessage> Messages { get; }
    }
}