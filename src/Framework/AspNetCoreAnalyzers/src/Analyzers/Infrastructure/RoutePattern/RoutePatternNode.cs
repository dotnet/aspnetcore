// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;
using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal abstract class RoutePatternNode : EmbeddedSyntaxNode<RoutePatternKind, RoutePatternNode>
{
    protected RoutePatternNode(RoutePatternKind kind) : base(kind)
    {
    }

    public abstract void Accept(IRoutePatternNodeVisitor visitor);
}
