// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Describes a failure compiling a specific file.
/// </summary>
public class CompilationFailure
{
    /// <summary>
    /// Initializes a new instance of <see cref="CompilationFailure"/>.
    /// </summary>
    /// <param name="sourceFilePath">Path for the file that produced the compilation failure.</param>
    /// <param name="sourceFileContent">Contents of the file being compiled.</param>
    /// <param name="compiledContent">For templated languages (such as Asp.Net Core Razor), the generated content.
    /// </param>
    /// <param name="messages">One or or more <see cref="DiagnosticMessage"/> instances.</param>
    public CompilationFailure(
        string? sourceFilePath,
        string? sourceFileContent,
        string? compiledContent,
        IEnumerable<DiagnosticMessage>? messages)
        : this(sourceFilePath, sourceFileContent, compiledContent, messages, failureSummary: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="CompilationFailure"/>.
    /// </summary>
    /// <param name="sourceFilePath">Path for the file that produced the compilation failure.</param>
    /// <param name="sourceFileContent">Contents of the file being compiled.</param>
    /// <param name="compiledContent">For templated languages (such as Asp.Net Core Razor), the generated content.
    /// </param>
    /// <param name="messages">One or or more <see cref="DiagnosticMessage"/> instances.</param>
    /// <param name="failureSummary">Summary message or instructions to fix the failure.</param>
    public CompilationFailure(
        string? sourceFilePath,
        string? sourceFileContent,
        string? compiledContent,
        IEnumerable<DiagnosticMessage?>? messages,
        string? failureSummary)
    {
        SourceFilePath = sourceFilePath;
        SourceFileContent = sourceFileContent;
        CompiledContent = compiledContent;
        Messages = messages;
        FailureSummary = failureSummary;
    }

    /// <summary>
    /// Path of the file that produced the compilation failure.
    /// </summary>
    public string? SourceFilePath { get; }

    /// <summary>
    /// Contents of the file.
    /// </summary>
    public string? SourceFileContent { get; }

    /// <summary>
    /// Contents being compiled.
    /// </summary>
    /// <remarks>
    /// For templated files, the <see cref="SourceFileContent"/> represents the original content and
    /// <see cref="CompiledContent"/> represents the transformed content. This property can be null if
    /// the exception is encountered during transformation.
    /// </remarks>
    public string? CompiledContent { get; }

    /// <summary>
    /// Gets a sequence of <see cref="DiagnosticMessage"/> produced as a result of compilation.
    /// </summary>
    public IEnumerable<DiagnosticMessage?>? Messages { get; }

    /// <summary>
    /// Summary message or instructions to fix the failure.
    /// </summary>
    public string? FailureSummary { get; }
}
