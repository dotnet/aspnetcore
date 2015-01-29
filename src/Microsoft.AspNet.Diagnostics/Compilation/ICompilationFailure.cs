// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Specifies the contract for a file that fails compilation.
    /// </summary>
    [AssemblyNeutral]
    public interface ICompilationFailure
    {
        /// <summary>
        /// Path of the file that produced the compilation exception.
        /// </summary>
        string SourceFilePath { get; }

        /// <summary>
        /// Contents of the file.
        /// </summary>
        string SourceFileContent { get; }

        /// <summary>
        /// Contents being compiled.
        /// </summary>
        /// <remarks>
        /// For templated files, the <see cref="SourceFileContent"/> represents the original content and
        /// <see cref="CompiledContent"/> represents the transformed content. This property can be null if
        /// the exception is encountered during transformation.
        /// </remarks>
        string CompiledContent { get; }

        /// <summary>
        /// Gets a sequence of <see cref="ICompilationMessage"/> produced as a result of compilation.
        /// </summary>
        IEnumerable<ICompilationMessage> Messages { get; }
    }
}