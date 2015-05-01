// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="Exception"/> thrown when accessing the result of a failed compilation.
    /// </summary>
    public class CompilationFailedException : Exception, ICompilationException
    {
        /// <summary>
        /// Instantiates a new instance of <see cref="CompilationFailedException"/>.
        /// </summary>
        /// <param name="compilationFailures"><see cref="ICompilationFailure"/>s containing
        /// details of the compilation failure.</param>
        public CompilationFailedException(
                [NotNull] IEnumerable<ICompilationFailure> compilationFailures)
            : base(FormatMessage(compilationFailures))
        {
            CompilationFailures = compilationFailures;
        }

        /// <inheritdoc />
        public IEnumerable<ICompilationFailure> CompilationFailures { get; }

        private static string FormatMessage(IEnumerable<ICompilationFailure> compilationFailures)
        {
            return Resources.CompilationFailed + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    compilationFailures.SelectMany(f => f.Messages).Select(message => message.FormattedMessage));
        }
    }
}
