// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Performs necessary modifications to the <see cref="Intermediate.DocumentIntermediateNode"/> to complete and
/// optimize code generation.
/// </summary>
/// <remarks>
/// <para>
/// The last phase of intermediate node document processing is optimization. Passes in this phase perform some
/// kind of transformation on the intermediate node document that optimizes the generated code. The key distinction
/// here is that information may be discarded during this phase.
/// </para>
/// <para>
/// <see cref="IRazorOptimizationPass"/> objects are executed according to an ascending ordering of the
/// <see cref="IRazorOptimizationPass.Order"/> property.
/// </para>
/// </remarks>
public interface IRazorOptimizationPhase : IRazorEnginePhase
{
}
