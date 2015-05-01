// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// <see cref="ICompilationFailure"/> for Razor parse failures.
    /// </summary>
    public class RazorCompilationFailure : ICompilationFailure
    {
        /// <summary>Initializes a new instance of <see cref="RazorCompilationFailure"/>.</summary>
        /// <param name="sourceFilePath">The path of the Razor source file that was compiled.</param>
        /// <param name="sourceFileContent">The contents of the Razor source file.</param>
        /// <param name="messages">A sequence of <see cref="ICompilationMessage"/> encountered
        /// during compilation.</param>
        public RazorCompilationFailure(
            [NotNull] string sourceFilePath,
            [NotNull] string sourceFileContent,
            [NotNull] IEnumerable<RazorCompilationMessage> messages)
        {
            SourceFilePath = sourceFilePath;
            SourceFileContent = sourceFileContent;
            Messages = messages;
        }

        /// <inheritdoc />
        public string SourceFilePath { get; }

        /// <inheritdoc />
        public string SourceFileContent { get; }

        /// <inheritdoc />
        public string CompiledContent { get; } = null;

        /// <inheritdoc />
        public IEnumerable<ICompilationMessage> Messages { get; }
    }
}