// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An exception thrown when accessing the result of a failed compilation.
    /// </summary>
    public class CompilationFailedException : Exception, ICompilationException
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="CompilationFailedException"/>.
        /// </summary>
        /// <param name="compilationFailure">The <see cref="ICompilationFailure"/> instance containing
        /// details of the compilation failure.</param>
        public CompilationFailedException(
                [NotNull] ICompilationFailure compilationFailure)
            : base(Resources.FormatCompilationFailed(compilationFailure.SourceFilePath))
        {
            CompilationFailures = new[] { compilationFailure };
        }

        /// <inheritdoc />
        public IEnumerable<ICompilationFailure> CompilationFailures { get; }
    }
}
