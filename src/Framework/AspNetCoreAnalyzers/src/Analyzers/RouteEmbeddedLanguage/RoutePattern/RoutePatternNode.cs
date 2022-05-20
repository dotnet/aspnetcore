// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;

internal abstract class RoutePatternNode : EmbeddedSyntaxNode<RoutePatternKind, RoutePatternNode>
{
    protected RoutePatternNode(RoutePatternKind kind) : base(kind)
    {
    }

    public abstract void Accept(IRoutePatternNodeVisitor visitor);
}
