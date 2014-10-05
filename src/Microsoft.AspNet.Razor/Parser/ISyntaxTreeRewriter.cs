// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Parser
{
    /// <summary>
    /// Contract for rewriting a syntax tree.
    /// </summary>
    public interface ISyntaxTreeRewriter
    {
        /// <summary>
        /// Rewrites the provided <paramref name="context"/>s <see cref= "RewritingContext.SyntaxTree" />.
        /// </summary>
        /// <param name="context">Contains information on the rewriting of the syntax tree.</param>
        /// <remarks>
        /// To modify the syntax tree replace the <paramref name="context"/>s <see cref="RewritingContext.SyntaxTree"/>.
        /// </remarks>
        void Rewrite(RewritingContext context);
    }
}
