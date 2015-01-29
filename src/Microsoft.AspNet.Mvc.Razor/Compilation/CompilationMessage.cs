// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Diagnostics;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents a message encountered during compilation.
    /// </summary>
    public class CompilationMessage : ICompilationMessage
    {
        /// <summary>
        /// Initializes a <see cref="CompilationMessage"/> with the specified message.
        /// </summary>
        /// <param name="message">A message <see cref="string"/> produced from compilation.</param>
        public CompilationMessage(string message,
                                  int startColumn,
                                  int startLine,
                                  int endColumn,
                                  int endLine)
        {
            Message = message;
            StartColumn = startColumn;
            StartLine = startLine;
            EndColumn = endColumn;
            EndLine = endLine;
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

        /// <summary>
        /// Returns a <see cref="string"/> representation of this instance of <see cref="CompilationMessage"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representing this <see cref="CompilationMessage"/> instance.</returns>
        /// <remarks>Returns same value as <see cref="Message"/>.</remarks>
        public override string ToString()
        {
            return Message;
        }
    }
}
