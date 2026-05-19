// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

/// <summary>
/// Represents an invocation of the AddOpenApi method.
/// </summary>
/// <param name="Variant">The variant of the AddOpenApi method.</param>
/// <param name="InvocationExpression">The invocation expression.</param>
/// <param name="Location">The location of the invocation that can be intercepted.</param>
internal sealed record AddOpenApiInvocation(
    AddOpenApiOverloadVariant Variant,
    InvocationExpressionSyntax InvocationExpression,
    InterceptableLocation? Location);
