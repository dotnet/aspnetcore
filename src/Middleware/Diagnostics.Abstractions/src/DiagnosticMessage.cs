// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// A single diagnostic message.
    /// </summary>
    public class DiagnosticMessage
    {
        public DiagnosticMessage(
            string message,
            string formattedMessage,
            string filePath,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Message = message;
            SourceFilePath = filePath;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
            FormattedMessage = formattedMessage;
        }

        /// <summary>
        /// Path of the file that produced the message.
        /// </summary>
        public string SourceFilePath { get; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the one-based line index for the start of the compilation error.
        /// </summary>
        public int StartLine { get; }

        /// <summary>
        /// Gets the zero-based column index for the start of the compilation error.
        /// </summary>
        public int StartColumn { get; }

        /// <summary>
        /// Gets the one-based line index for the end of the compilation error.
        /// </summary>
        public int EndLine { get; }

        /// <summary>
        /// Gets the zero-based column index for the end of the compilation error.
        /// </summary>
        public int EndColumn { get; }

        /// <summary>
        /// Gets the formatted error message.
        /// </summary>
        public string FormattedMessage { get; }
    }
}