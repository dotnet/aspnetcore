// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.StackTrace.Sources;

namespace Microsoft.AspNetCore.Diagnostics.RazorViews;

/// <summary>
/// Holds data to be displayed on the compilation error page.
/// </summary>
internal sealed class CompilationErrorPageModel
{
    public CompilationErrorPageModel(DeveloperExceptionPageOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// Options for what output to display.
    /// </summary>
    public DeveloperExceptionPageOptions Options { get; }

    /// <summary>
    /// Detailed information about each parse or compilation error.
    /// </summary>
    public IList<ExceptionDetails> ErrorDetails { get; } = new List<ExceptionDetails>();

    /// <summary>
    /// Gets the generated content that produced the corresponding <see cref="ErrorDetails"/>.
    /// </summary>
    public IList<string?> CompiledContent { get; } = new List<string?>();
}
