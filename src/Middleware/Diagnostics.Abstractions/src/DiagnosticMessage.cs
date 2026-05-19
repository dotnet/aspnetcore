// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// A single diagnostic message.
/// </summary>
public class DiagnosticMessage
{
    /// <summary>
    /// Initializes a new instance of <see cref="DiagnosticMessage"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="formattedMessage">The formatted error message.</param>
    /// <param name="filePath">The path of the file that produced the message.</param>
    /// <param name="startLine">The one-based line index for the start of the compilation error.</param>
    /// <param name="startColumn">The zero-based column index for the start of the compilation error.</param>
    /// <param name="endLine">The one-based line index for the end of the compilation error.</param>
    /// <param name="endColumn">The zero-based column index for the end of the compilation error.</param>
    public DiagnosticMessage(
        string? message,
        string? formattedMessage,
        string? filePath,
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
    public string? SourceFilePath { get; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string? Message { get; }

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
    public string? FormattedMessage { get; }
}
