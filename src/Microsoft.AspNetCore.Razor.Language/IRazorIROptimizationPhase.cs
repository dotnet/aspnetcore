// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Performs necessary modifications to the IR document to optimize code generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The last phase of IR processing is lowering. IR passes in this phase perform some kind of transformation
    /// on the IR that optimizes the generated code. The key distinction here is that information may be discarded 
    /// during this phase.
    /// </para>
    /// <para>
    /// <see cref="IRazorIROptimizationPass"/> objects are executed according to an ascending ordering of the
    /// <see cref="IRazorIROptimizationPass.Order"/> property. The default configuration of <see cref="RazorEngine"/>
    /// prescribes a logical ordering of specific phases of IR processing.
    /// </para>
    /// </remarks>
    public interface IRazorIROptimizationPhase : IRazorEnginePhase
    {
    }
}