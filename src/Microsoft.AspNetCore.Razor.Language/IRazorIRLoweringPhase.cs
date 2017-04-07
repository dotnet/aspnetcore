// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Generates the IR document from <see cref="RazorSyntaxTree"/>.
    /// </summary>
    /// <remarks>
    /// The IR document is first produced by <see cref="IRazorIRLoweringPhase"/>. At this point no IR passes have
    /// been executed. The default <see cref="IRazorIRLoweringPhase"/> will perform a mechanical transformation
    /// of the syntax tree to IR resulting in a mostly flat structure. It is up to later phases to give the document
    /// structure and semantics according to a document kind. The default <see cref="IRazorIRLoweringPhase"/> is
    /// also responsible for synthesizing IR nodes for global cross-current concerns such as checksums or global settings.
    /// </remarks>
    public interface IRazorIRLoweringPhase : IRazorEnginePhase
    {
    }
}
