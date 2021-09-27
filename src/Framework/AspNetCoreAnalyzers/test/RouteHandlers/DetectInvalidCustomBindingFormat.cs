// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Testing;
using VerifyCS = Microsoft.AspNetCore.Analyzers.RouteHandlers.CSharpRouteHandlerCodeFixVerifier<
    Microsoft.AspNetCore.Analyzers.RouteHandlers.RouteHandlerAnalyzer,
    Microsoft.AspNetCore.Analyzers.RouteHandlers.Fixers.DetectMismatchedParameterOptionalityFixer>;

namespace Microsoft.AspNetCore.Analyzers.RouteHandlers;

public class DetectInvalidCustomBindingFormat
{

}
