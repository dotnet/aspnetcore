// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    /// <summary>
    /// Used to manage <see cref="RazorDiagnostic"/>s encountered during the Razor parsing phase.
    /// </summary>
    internal class ErrorSink
    {
        private readonly List<RazorDiagnostic> _errors;

        /// <summary>
        /// Instantiates a new instance of <see cref="ErrorSink"/>.
        /// </summary>
        public ErrorSink()
        {
            _errors = new List<RazorDiagnostic>();
        }

        /// <summary>
        /// <see cref="RazorDiagnostic"/>s collected.
        /// </summary>
        public IReadOnlyList<RazorDiagnostic> Errors => _errors;

        /// <summary>
        /// Tracks the given <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The <see cref="RazorError"/> to track.</param>
        public void OnError(RazorDiagnostic error) =>_errors.Add(error);

        /// <summary>
        /// Creates and tracks a new <see cref="RazorDiagnostic"/>.
        /// </summary>
        /// <param name="location"><see cref="SourceLocation"/> of the error.</param>
        /// <param name="message">A message describing the error.</param>
        /// <param name="length">The length of the error.</param>
        /// <remarks>This is temporary. It will be removed once we get rid of <see cref="LegacyRazorDiagnostic"/>.</remarks>
        public void OnError(SourceLocation location, string message, int length)
        {
            var error = RazorDiagnostic.Create(new RazorError(message, location, length));
            _errors.Add(error);
        }
    }
}