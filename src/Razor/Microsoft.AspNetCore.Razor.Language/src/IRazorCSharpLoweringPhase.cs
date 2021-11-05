// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Generates C# code using the intermediate node document.
/// </summary>
/// <remarks>
/// After IR processing, the <see cref="IRazorCSharpLoweringPhase"/> transforms the intermediate node document into
/// generated C# code. At this time any directives or other constructs that cannot be understood by code generation
/// will result in an error.
/// </remarks>
public interface IRazorCSharpLoweringPhase : IRazorEnginePhase
{
}
