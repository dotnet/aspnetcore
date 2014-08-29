// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Parser
{
    /// <summary>
    /// Defines the contract for rewriting a syntax tree.
    /// </summary>
    public interface ISyntaxTreeRewriter
    {
        /// <summary>
        /// Rewrites the provided <paramref name="input"/> syntax tree.
        /// </summary>
        /// <param name="input">The current syntax tree.</param>
        /// <returns>The <paramref name="input"/> syntax tree or a syntax tree to be used instead of the 
        /// <paramref name="input"/> tree.</returns>
        /// <remarks>
        /// If you choose not to modify the syntax tree you can always return <paramref name="input"/>.
        /// </remarks>
        Block Rewrite(Block input);
    }
}
