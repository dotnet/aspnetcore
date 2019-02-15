// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal class CompilationFailedException : Exception, ICompilationException
    {
        public CompilationFailedException(
                IEnumerable<CompilationFailure> compilationFailures)
            : base(FormatMessage(compilationFailures))
        {
            if (compilationFailures == null)
            {
                throw new ArgumentNullException(nameof(compilationFailures));
            }

            CompilationFailures = compilationFailures;
        }

        public IEnumerable<CompilationFailure> CompilationFailures { get; }

        private static string FormatMessage(IEnumerable<CompilationFailure> compilationFailures)
        {
            return Resources.CompilationFailed + Environment.NewLine +
                string.Join(
                    Environment.NewLine,
                    compilationFailures.SelectMany(f => f.Messages).Select(message => message.FormattedMessage));
        }
    }
}