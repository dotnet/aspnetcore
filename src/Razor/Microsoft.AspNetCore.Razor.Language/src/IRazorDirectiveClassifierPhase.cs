// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Understands directive nodes and performs the necessary modifications to the document.
/// </summary>
/// <remarks>
/// <para>
/// The second phase of intermediate node processing is directive classification. Passes in this phase should interpret
/// directives and processing them accordingly by transforming nodes or adding diagnostics. At this time the document
/// kind has been identified, so any directive that can't be applied should trigger
/// errors. If implementing a document kind that diverges from the standard structure of Razor documents
/// it may be necessary to reimplement processing of default directives.
/// </para>
/// <para>
/// <see cref="IRazorDirectiveClassifierPass"/> objects are executed according to an ascending ordering of the
/// <see cref="IRazorDirectiveClassifierPass.Order"/> property.
/// </para>
/// </remarks>
public interface IRazorDirectiveClassifierPhase : IRazorEnginePhase
{
}
