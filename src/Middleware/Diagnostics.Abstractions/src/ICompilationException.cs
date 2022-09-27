// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Specifies the contract for an exception representing compilation failure.
/// </summary>
/// <remarks>
/// This interface is implemented on exceptions thrown during compilation to enable consumers
/// to read compilation-related data out of the exception
/// </remarks>
public interface ICompilationException
{
    /// <summary>
    /// Gets a sequence of <see cref="CompilationFailure"/> with compilation failures.
    /// </summary>
    IEnumerable<CompilationFailure?>? CompilationFailures { get; }
}
