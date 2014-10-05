// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Parser
{
    /// <summary>
    /// Informational class for rewriting a syntax tree.
    /// </summary>
    public class RewritingContext
    {
        private readonly List<RazorError> _errors;

        /// <summary>
        /// Instantiates a new <see cref="RewritingContext"/>.
        /// </summary>
        public RewritingContext(Block syntaxTree)
        {
            _errors = new List<RazorError>();
            SyntaxTree = syntaxTree;
        }

        /// <summary>
        /// The documents syntax tree.
        /// </summary>
        public Block SyntaxTree { get; set; }

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
        /// Creates and tracks a new <see cref="RazorError"/>.
        /// </summary>
        /// <param name="location"><see cref="SourceLocation"/> of the error.</param>
        /// <param name="message">A message describing the error.</param>        
        public void OnError(SourceLocation location, string message)
        {
            _errors.Add(new RazorError(message, location));
        }
    }
}