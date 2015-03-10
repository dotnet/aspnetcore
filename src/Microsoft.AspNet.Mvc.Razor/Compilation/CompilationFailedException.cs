// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// An <see cref="Exception"/> thrown when accessing the result of a failed compilation.
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
            : base(FormatMessage(compilationFailure))
        {
            CompilationFailures = new[] { compilationFailure };
        }

        /// <inheritdoc />
        public IEnumerable<ICompilationFailure> CompilationFailures { get; }

        private static string FormatMessage(ICompilationFailure compilationFailure)
        {
            return Resources.CompilationFailed + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    compilationFailure.Messages.Select(message => message.FormattedMessage));
        }
    }
}
