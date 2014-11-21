// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An exception thrown when accessing the result of a failed compilation.
    /// </summary>
    public class CompilationFailedException : Exception
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="CompilationFailedException"/>.
        /// </summary>
        /// <param name="filePath">The path of the Razor source file that was compiled.</param>
        /// <param name="fileContent">The contents of the Razor source file.</param>
        /// <param name="compiledContent">The generated C# content that was compiled.</param>
        /// <param name="messages">A sequence of <see cref="CompilationMessage"/> encountered
        /// during compilation.</param>
        public CompilationFailedException(
                [NotNull] string filePath,
                [NotNull] string fileContent,
                [NotNull] string compiledContent,
                [NotNull] IEnumerable<CompilationMessage> messages)
            : base(FormatMessage(messages))
        {
            FilePath = filePath;
            FileContent = fileContent;
            CompiledContent = compiledContent;
            Messages = messages.ToList();
        }

        /// <summary>
        /// Gets the path of the Razor source file that produced the compilation failure.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets a sequence of <see cref="CompilationMessage"/> instances encountered during compilation.
        /// </summary>
        public IEnumerable<CompilationMessage> Messages { get; private set; }

        /// <summary>
        /// Gets the content of the Razor source file.
        /// </summary>
        public string FileContent { get; private set; }

        /// <summary>
        /// Gets the generated C# content that was compiled.
        /// </summary>
        public string CompiledContent { get; private set; }

        /// <inheritdoc />
        public override string Message
        {
            get
            {
                return Resources.FormatCompilationFailed(FilePath) +
                       Environment.NewLine +
                       FormatMessage(Messages);
            }
        }

        private static string FormatMessage(IEnumerable<CompilationMessage> messages)
        {
            return string.Join(Environment.NewLine, messages);
        }
    }
}
