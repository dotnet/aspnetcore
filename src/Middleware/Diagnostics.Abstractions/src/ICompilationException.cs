// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Diagnostics
{
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
        IEnumerable<CompilationFailure> CompilationFailures { get; }
    }
}