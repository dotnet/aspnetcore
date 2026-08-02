// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class CompilationFailedException : Exception, ICompilationException
{
    public CompilationFailedException(
            IEnumerable<CompilationFailure> compilationFailures)
        : base(FormatMessage(compilationFailures))
    {
        ArgumentNullException.ThrowIfNull(compilationFailures);

        CompilationFailures = compilationFailures;
    }

    public IEnumerable<CompilationFailure> CompilationFailures { get; }

    private static string FormatMessage(IEnumerable<CompilationFailure> compilationFailures)
    {
        return Resources.CompilationFailed + Environment.NewLine +
            string.Join(
                Environment.NewLine,
                compilationFailures.SelectMany(f => f.Messages!).Select(message => message!.FormattedMessage));
    }
}
