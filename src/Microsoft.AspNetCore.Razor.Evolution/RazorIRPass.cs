// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    /// <summary>
    /// Provides constants for ordering of <see cref="IRazorIRPass"/> objects. When implementing an
    /// <see cref="IRazorIRPass"/>, choose a value for <see cref="IRazorIRPass.Order"/> according to
    /// the logical task that must be performed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IRazorIRPass"/> objects are executed according to an ascending ordering of the
    /// <see cref="IRazorIRPass.Order"/> property. The default configuration of <see cref="RazorEngine"/>
    /// prescribes a logical ordering of specific phases of IR processing.
    /// </para>
    /// <para>
    /// The IR document is first produced by <see cref="IRazorIRLoweringPhase"/>. At this point no IR passes have
    /// been executed. The default <see cref="IRazorIRLoweringPhase"/> will perform a mechanical transformation
    /// of the syntax tree to IR resulting in a mostly flat structure. It is up to later phases to give the document
    /// structure and semantics according to a document kind. The default <see cref="IRazorIRLoweringPhase"/> is
    /// also responsible for synthesizing IR nodes for global cross-current concerns such as checksums or global settings.
    /// </para>
    /// <para>
    /// The first phase of IR procesing is document classification. IR passes in this phase should classify the
    /// document according to any relevant criteria (project configuration, file extension, directive) and modify
    /// the IR tree to suit the desired document shape. Document classifiers should also set
    /// <see cref="DocumentIRNode.DocumentKind"/> to prevent other classifiers from running. If no classifier 
    /// matches the document, then it will be classified as &quot;generic&quot; and processed according to set 
    /// of reasonable defaults.
    /// </para>
    /// <para>
    /// The second phase of IR processing is directive classification. IR passes in this phase should interpret 
    /// directives and processing them accordingly by transforming IR nodes or adding diagnostics to the IR. At
    /// this time the document kind has been identified, so any directive that can't be applied should trigger
    /// errors. If implementing a document kind that diverges from the standard structure of Razor documents
    /// it may be necessary to reimplement processing of default directives.
    /// </para>
    /// <para>
    /// The last phase of IR processing is lowering. IR passes in this phase perform some kind of transformation
    /// on the IR that optimizes the generated code. The key distinction here is that information may be discarded 
    /// during this phase.
    /// </para>
    /// <para>
    /// Finally, the <see cref="IRazorCSharpLoweringPhase"/> transforms the IR document into generated C# code.
    /// At this time any directives or IR constructs that cannot be understood by code generation will result
    /// in an error.
    /// </para>
    /// </remarks>
    public static class RazorIRPass
    {
        /// <summary>
        /// An <see cref="IRazorIRPass"/> that implements a document classifier should use this value as its
        /// <see cref="IRazorIRPass.Order"/>.
        /// </summary>               
        public static readonly int DocumentClassifierOrder = 1100;

        /// <summary>
        /// <see cref="IRazorIRPass.Order"/> value used by the default document classifier.
        /// </summary>
        public static readonly int DefaultDocumentClassifierOrder = 1900;

        /// <summary>
        /// An <see cref="IRazorIRPass"/> that implements a directive classifier should use this value as its
        /// <see cref="IRazorIRPass.Order"/>.
        /// </summary>    
        public static readonly int DirectiveClassifierOrder = 2100;

        /// <summary>
        /// <see cref="IRazorIRPass.Order"/> value used by the default directive classifier.
        /// </summary>
        public static readonly int DefaultDirectiveClassifierOrder = 2900;

        /// <summary>
        /// An <see cref="IRazorIRPass"/> that implements a lowering phase should use this value as its
        /// <see cref="IRazorIRPass.Order"/>.
        /// </summary>    
        public static readonly int LoweringOrder = 4100;

        /// <summary>
        /// <see cref="IRazorIRPass.Order"/> value used by the default lowering phase.
        /// </summary>
        public static readonly int DefaultLoweringOrder = 4900;
    }
}
