// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// <see cref="ICompilationMessage"/> for a <see cref="RazorError"/> encountered during parsing.
    /// </summary>
    public class RazorCompilationMessage : ICompilationMessage
    {
        /// <summary>
        /// Initializes a <see cref="RazorCompilationMessage"/> with the specified message.
        /// </summary>
        /// <param name="razorError">A <see cref="RazorError"/>.</param>
        /// <param name="sourceFilePath">The path of the Razor source file that was parsed.</param>
        public RazorCompilationMessage(
            [NotNull] RazorError razorError,
            string sourceFilePath)
        {
            SourceFilePath = sourceFilePath;
            Message = razorError.Message;

            var location = razorError.Location;
            FormattedMessage =
                $"{sourceFilePath} ({location.LineIndex},{location.CharacterIndex}) {razorError.Message}";

            StartColumn = location.CharacterIndex;
            StartLine = location.LineIndex + 1;
            EndColumn = location.CharacterIndex + razorError.Length;
            EndLine = location.LineIndex + 1;
        }

        /// <summary>
        /// Gets a message produced from compilation.
        /// </summary>
        public string Message { get; }

        /// <inheritdoc />
        public int StartColumn { get; }

        /// <inheritdoc />
        public int StartLine { get; }

        /// <inheritdoc />
        public int EndColumn { get; }

        /// <inheritdoc />
        public int EndLine { get; }

        /// <inheritdoc />
        public string SourceFilePath { get; }

        /// <inheritdoc />
        public string FormattedMessage { get; }

        /// <inheritdoc />
        // All Razor diagnostics are errors
        public CompilationMessageSeverity Severity { get; } = CompilationMessageSeverity.Error;

        /// <summary>
        /// Returns a <see cref="string"/> representation of this instance of <see cref="RazorCompilationMessage"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representing this <see cref="RazorCompilationMessage"/> instance.</returns>
        /// <remarks>Returns same value as <see cref="Message"/>.</remarks>
        public override string ToString()
        {
            return FormattedMessage;
        }
    }
}
