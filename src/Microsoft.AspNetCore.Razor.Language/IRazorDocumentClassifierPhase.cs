// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Modifies the IR document to a desired structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first phase of IR procesing is document classification. IR passes in this phase should classify the
    /// document according to any relevant criteria (project configuration, file extension, directive) and modify
    /// the IR tree to suit the desired document shape. Document classifiers should also set
    /// <see cref="DocumentIRNode.DocumentKind"/> to prevent other classifiers from running. If no classifier 
    /// matches the document, then it will be classified as &quot;generic&quot; and processed according to set 
    /// of reasonable defaults.
    /// </para>
    /// <para>
    /// <see cref="IRazorDocumentClassifierPass"/> objects are executed according to an ascending ordering of the
    /// <see cref="IRazorDocumentClassifierPass.Order"/> property. The default configuration of <see cref="RazorEngine"/>
    /// prescribes a logical ordering of specific phases of IR processing.
    /// </para>
    /// </remarks>
    public interface IRazorDocumentClassifierPhase : IRazorEnginePhase
    {
    }
}