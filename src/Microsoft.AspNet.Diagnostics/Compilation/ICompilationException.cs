// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Specifies the contract for an exception representing compilation failure.
    /// </summary>
    [AssemblyNeutral]
    public interface ICompilationException
    {
        /// <summary>
        /// Gets a sequence of <see cref="ICompilationFailure"/> with compilation failures.
        /// </summary>
        IEnumerable<ICompilationFailure> CompilationFailures { get; }
    }
}