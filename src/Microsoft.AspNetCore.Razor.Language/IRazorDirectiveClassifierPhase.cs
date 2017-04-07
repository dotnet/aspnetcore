// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Understands directive IR nodes and performs the necessary modifications to the IR document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The second phase of IR processing is directive classification. IR passes in this phase should interpret 
    /// directives and processing them accordingly by transforming IR nodes or adding diagnostics to the IR. At
    /// this time the document kind has been identified, so any directive that can't be applied should trigger
    /// errors. If implementing a document kind that diverges from the standard structure of Razor documents
    /// it may be necessary to reimplement processing of default directives.
    /// </para>
    /// <para>
    /// <see cref="IRazorDirectiveClassifierPass"/> objects are executed according to an ascending ordering of the
    /// <see cref="IRazorDirectiveClassifierPass.Order"/> property. The default configuration of <see cref="RazorEngine"/>
    /// prescribes a logical ordering of specific phases of IR processing.
    /// </para>
    /// </remarks>
    public interface IRazorDirectiveClassifierPhase : IRazorEnginePhase
    {
    }
}