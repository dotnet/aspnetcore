// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Generates the intermediate node document from <see cref="RazorSyntaxTree"/>.
    /// </summary>
    /// <remarks>
    /// The document is first produced by <see cref="IRazorIntermediateNodeLoweringPhase"/>. At this point no intermediate node
    /// passes have been executed. The default <see cref="IRazorIntermediateNodeLoweringPhase"/> will perform a mechanical
    /// transformation of the syntax tree to intermediate nodes, resulting in a mostly flat structure. It is up to later phases 
    /// to give the document structure and semantics according to a document kind. The default 
    /// <see cref="IRazorIntermediateNodeLoweringPhase"/> is also responsible for merging nodes from imported documents.
    /// </remarks>
    public interface IRazorIntermediateNodeLoweringPhase : IRazorEnginePhase
    {
    }
}
