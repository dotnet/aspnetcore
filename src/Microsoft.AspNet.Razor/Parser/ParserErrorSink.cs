// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser
{
    /// <summary>
    /// Used to manage <see cref="RazorError"/>s encountered during the Razor parsing phase.
    /// </summary>
    public class ParserErrorSink
    {
        private readonly List<RazorError> _errors;

        /// <summary>
        /// Instantiates a new instance of <see cref="ParserErrorSink"/>.
        /// </summary>
        public ParserErrorSink()
        {
            _errors = new List<RazorError>();
        }

        /// <summary>
        /// <see cref="RazorError"/>s collected.
        /// </summary>
        public IEnumerable<RazorError> Errors
        {
            get
            {
                return _errors;
            }
        }

        /// <summary>
        /// Tracks the given <paramref name="error"/>.
        /// </summary>
        /// <param name="error">The <see cref="RazorError"/> to track.</param>        
        public void OnError(RazorError error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// Creates and tracks a new <see cref="RazorError"/>.
        /// </summary>
        /// <param name="location"><see cref="SourceLocation"/> of the error.</param>
        /// <param name="message">A message describing the error.</param>        
        public void OnError(SourceLocation location, string message)
        {
            _errors.Add(new RazorError(message, location));
        }

        /// <summary>
        /// Creates and tracks a new <see cref="RazorError"/>.
        /// </summary>
        /// <param name="location"><see cref="SourceLocation"/> of the error.</param>
        /// <param name="message">A message describing the error.</param>
        /// <param name="length">The length of the error.</param>
        public void OnError(SourceLocation location, string message, int length)
        {
            _errors.Add(new RazorError(message, location, length));
        }
    }
}