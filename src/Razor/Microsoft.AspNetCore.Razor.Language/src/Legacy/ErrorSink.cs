// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

/// <summary>
/// Used to manage <see cref="RazorDiagnostic"/>s encountered during the Razor parsing phase.
/// </summary>
internal class ErrorSink
{
    private readonly List<RazorDiagnostic> _errors;

    /// <summary>
    /// Instantiates a new instance of <see cref="ErrorSink"/>.
    /// </summary>
    public ErrorSink()
    {
        _errors = new List<RazorDiagnostic>();
    }

    /// <summary>
    /// <see cref="RazorDiagnostic"/>s collected.
    /// </summary>
    public IReadOnlyList<RazorDiagnostic> Errors => _errors;

    /// <summary>
    /// Tracks the given <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The <see cref="RazorDiagnostic"/> to track.</param>
    public void OnError(RazorDiagnostic error) => _errors.Add(error);
}
