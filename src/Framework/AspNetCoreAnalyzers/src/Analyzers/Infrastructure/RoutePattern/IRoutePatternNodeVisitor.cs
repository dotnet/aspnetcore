// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal interface IRoutePatternNodeVisitor
{
    void Visit(RoutePatternCompilationUnit node);
    void Visit(RoutePatternSegmentNode node);
    void Visit(RoutePatternReplacementNode node);
    void Visit(RoutePatternParameterNode node);
    void Visit(RoutePatternLiteralNode node);
    void Visit(RoutePatternSegmentSeparatorNode node);
    void Visit(RoutePatternOptionalSeparatorNode node);
    void Visit(RoutePatternCatchAllParameterPartNode node);
    void Visit(RoutePatternNameParameterPartNode node);
    void Visit(RoutePatternPolicyParameterPartNode node);
    void Visit(RoutePatternPolicyFragmentEscapedNode node);
    void Visit(RoutePatternPolicyFragment node);
    void Visit(RoutePatternOptionalParameterPartNode node);
    void Visit(RoutePatternDefaultValueParameterPartNode node);
}
