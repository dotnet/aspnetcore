// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Specifies the contract for diagnostic messages produced as result of compiling an instance
    /// of <see cref="ICompilationFailure"/>.
    /// </summary>
    [AssemblyNeutral]
    public interface ICompilationMessage
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        string Message { get; }

        /// <summary>
        /// Gets the zero-based line index for the start of the compilation error.
        /// </summary>
        int StartLine { get; }

        /// <summary>
        /// Gets the zero-based column index for the start of the compilation error.
        /// </summary>
        int StartColumn { get; }

        /// <summary>
        /// Gets the zero-based line index for the end of the compilation error.
        /// </summary>
        int EndLine { get; }

        /// <summary>
        /// Gets the zero-based column index for the end of the compilation error.
        /// </summary>
        int EndColumn { get; }
    }
}