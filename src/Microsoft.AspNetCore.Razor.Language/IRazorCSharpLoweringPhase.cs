// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Generates C# code using the IR document.
    /// </summary>
    /// <remarks>
    /// After IR processing, the <see cref="IRazorCSharpLoweringPhase"/> transforms the IR document into generated C# code.
    /// At this time any directives or IR constructs that cannot be understood by code generation will result
    /// in an error.
    /// </remarks>
    public interface IRazorCSharpLoweringPhase : IRazorEnginePhase
    {
    }
}
