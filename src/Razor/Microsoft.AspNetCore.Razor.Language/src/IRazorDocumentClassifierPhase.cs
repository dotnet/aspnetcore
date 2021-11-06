// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Modifies the intermediate node document to a desired structure.
/// </summary>
/// <remarks>
/// <para>
/// The first phase of intermediate node procesing is document classification. Passes in this phase should classify the
/// document according to any relevant criteria (project configuration, file extension, directive) and modify
/// the intermediate node document to suit the desired document shape. Document classifiers should also set
/// <see cref="DocumentIntermediateNode.DocumentKind"/> to prevent other classifiers from running. If no classifier
/// matches the document, then it will be classified as &quot;generic&quot; and processed according to set
/// of reasonable defaults.
/// </para>
/// <para>
/// <see cref="IRazorDocumentClassifierPass"/> objects are executed according to an ascending ordering of the
/// <see cref="IRazorDocumentClassifierPass.Order"/> property.
/// </para>
/// </remarks>
public interface IRazorDocumentClassifierPhase : IRazorEnginePhase
{
}
