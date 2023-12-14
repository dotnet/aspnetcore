// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Analyzers.Infrastructure.EmbeddedSyntax;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

using RoutePatternNodeOrToken = EmbeddedSyntaxNodeOrToken<RoutePatternKind, RoutePatternNode>;
using RoutePatternToken = EmbeddedSyntaxToken<RoutePatternKind>;

internal sealed class RoutePatternCompilationUnit : RoutePatternNode
{
    public RoutePatternCompilationUnit(ImmutableArray<RoutePatternRootPartNode> parts, RoutePatternToken endOfFileToken)
        : base(RoutePatternKind.CompilationUnit)
    {
        Debug.Assert(parts != null);
        Debug.Assert(endOfFileToken.Kind == RoutePatternKind.EndOfFile);
        Parts = parts;
        EndOfFileToken = endOfFileToken;
    }

    public ImmutableArray<RoutePatternRootPartNode> Parts { get; }
    public RoutePatternToken EndOfFileToken { get; }

    internal override int ChildCount => Parts.Length + 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
    {
        if (index == Parts.Length)
        {
            return EndOfFileToken;
        }

        return Parts[index];
    }

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternSegmentNode : RoutePatternRootPartNode
{
    public ImmutableArray<RoutePatternSegmentPartNode> Children { get; }

    internal override int ChildCount => Children.Length;

    public RoutePatternSegmentNode(ImmutableArray<RoutePatternSegmentPartNode> children)
        : base(RoutePatternKind.Segment)
    {
        Children = children;
    }

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => Children[index];

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

/// <summary>
/// [controller]
/// </summary>
internal sealed class RoutePatternReplacementNode : RoutePatternSegmentPartNode
{
    public RoutePatternReplacementNode(
        RoutePatternToken openBracketToken, RoutePatternToken textToken, RoutePatternToken closeBracketToken)
        : base(RoutePatternKind.Replacement)
    {
        Debug.Assert(openBracketToken.Kind == RoutePatternKind.OpenBracketToken);
        Debug.Assert(textToken.Kind == RoutePatternKind.ReplacementToken);
        Debug.Assert(closeBracketToken.Kind == RoutePatternKind.CloseBracketToken);
        OpenBracketToken = openBracketToken;
        TextToken = textToken;
        CloseBracketToken = closeBracketToken;
    }

    public RoutePatternToken OpenBracketToken { get; }
    public RoutePatternToken TextToken { get; }
    public RoutePatternToken CloseBracketToken { get; }

    internal override int ChildCount => 3;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => OpenBracketToken,
            1 => TextToken,
            2 => CloseBracketToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

/// <summary>
/// {controller}
/// </summary>
internal sealed class RoutePatternParameterNode : RoutePatternSegmentPartNode
{
    public RoutePatternParameterNode(
        RoutePatternToken openBraceToken, ImmutableArray<RoutePatternParameterPartNode> parameterPartNodes, RoutePatternToken closeBraceToken)
        : base(RoutePatternKind.Parameter)
    {
        Debug.Assert(openBraceToken.Kind == RoutePatternKind.OpenBraceToken);
        Debug.Assert(closeBraceToken.Kind == RoutePatternKind.CloseBraceToken);
        OpenBraceToken = openBraceToken;
        ParameterParts = parameterPartNodes;
        CloseBraceToken = closeBraceToken;
    }

    public RoutePatternToken OpenBraceToken { get; }
    public ImmutableArray<RoutePatternParameterPartNode> ParameterParts { get; }
    public RoutePatternToken CloseBraceToken { get; }

    internal override int ChildCount => ParameterParts.Length + 2;

    internal override RoutePatternNodeOrToken ChildAt(int index)
    {
        if (index == 0)
        {
            return OpenBraceToken;
        }
        else if (index == ParameterParts.Length + 1)
        {
            return CloseBraceToken;
        }
        else
        {
            return ParameterParts[index - 1];
        }
    }

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternLiteralNode : RoutePatternSegmentPartNode
{
    public RoutePatternLiteralNode(RoutePatternToken literalToken)
        : base(RoutePatternKind.Literal)
    {
        Debug.Assert(literalToken.Kind == RoutePatternKind.Literal);
        LiteralToken = literalToken;
    }

    public RoutePatternToken LiteralToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => LiteralToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternOptionalSeparatorNode : RoutePatternSegmentPartNode
{
    public RoutePatternOptionalSeparatorNode(RoutePatternToken separatorToken)
        : base(RoutePatternKind.Separator)
    {
        Debug.Assert(separatorToken.Kind == RoutePatternKind.DotToken);
        SeparatorToken = separatorToken;
    }

    public RoutePatternToken SeparatorToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => SeparatorToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternSegmentSeparatorNode : RoutePatternRootPartNode
{
    public RoutePatternSegmentSeparatorNode(RoutePatternToken separatorToken)
        : base(RoutePatternKind.Separator)
    {
        Debug.Assert(separatorToken.Kind == RoutePatternKind.SlashToken);
        SeparatorToken = separatorToken;
    }

    public RoutePatternToken SeparatorToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => SeparatorToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternCatchAllParameterPartNode : RoutePatternParameterPartNode
{
    public RoutePatternCatchAllParameterPartNode(RoutePatternToken asteriskToken)
        : base(RoutePatternKind.CatchAll)
    {
        Debug.Assert(asteriskToken.Kind == RoutePatternKind.AsteriskToken);
        AsteriskToken = asteriskToken;
    }

    public RoutePatternToken AsteriskToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => AsteriskToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternOptionalParameterPartNode : RoutePatternParameterPartNode
{
    public RoutePatternOptionalParameterPartNode(RoutePatternToken questionMarkToken)
        : base(RoutePatternKind.Optional)
    {
        Debug.Assert(questionMarkToken.Kind == RoutePatternKind.QuestionMarkToken);
        QuestionMarkToken = questionMarkToken;
    }

    public RoutePatternToken QuestionMarkToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => QuestionMarkToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternDefaultValueParameterPartNode : RoutePatternParameterPartNode
{
    public RoutePatternDefaultValueParameterPartNode(RoutePatternToken equalsToken, RoutePatternToken defaultValueToken)
        : base(RoutePatternKind.DefaultValue)
    {
        Debug.Assert(equalsToken.Kind == RoutePatternKind.EqualsToken);
        Debug.Assert(defaultValueToken.Kind == RoutePatternKind.DefaultValueToken);
        EqualsToken = equalsToken;
        DefaultValueToken = defaultValueToken;
    }

    public RoutePatternToken EqualsToken { get; }
    public RoutePatternToken DefaultValueToken { get; }

    internal override int ChildCount => 2;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => EqualsToken,
            1 => DefaultValueToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternNameParameterPartNode : RoutePatternParameterPartNode
{
    public RoutePatternNameParameterPartNode(RoutePatternToken parameterNameToken)
        : base(RoutePatternKind.ParameterName)
    {
        Debug.Assert(parameterNameToken.Kind == RoutePatternKind.ParameterNameToken);
        ParameterNameToken = parameterNameToken;
    }

    public RoutePatternToken ParameterNameToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => ParameterNameToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternPolicyParameterPartNode : RoutePatternParameterPartNode
{
    public RoutePatternPolicyParameterPartNode(RoutePatternToken colonToken, ImmutableArray<RoutePatternNode> policyFragments)
        : base(RoutePatternKind.ParameterPolicy)
    {
        Debug.Assert(colonToken.Kind == RoutePatternKind.ColonToken);
        ColonToken = colonToken;
        PolicyFragments = policyFragments;
    }

    public RoutePatternToken ColonToken { get; }
    public ImmutableArray<RoutePatternNode> PolicyFragments { get; }

    internal override int ChildCount => PolicyFragments.Length + 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => ColonToken,
            _ => PolicyFragments[index - 1],
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternPolicyFragmentEscapedNode : RoutePatternNode
{
    public RoutePatternPolicyFragmentEscapedNode(
        RoutePatternToken openParenToken, RoutePatternToken argumentToken, RoutePatternToken closeParenToken)
        : base(RoutePatternKind.PolicyFragmentEscaped)
    {
        Debug.Assert(openParenToken.Kind == RoutePatternKind.OpenParenToken);
        Debug.Assert(argumentToken.Kind == RoutePatternKind.PolicyFragmentToken);
        Debug.Assert(closeParenToken.Kind == RoutePatternKind.CloseParenToken);
        OpenParenToken = openParenToken;
        CloseParenToken = closeParenToken;
        ArgumentToken = argumentToken;
    }

    public RoutePatternToken OpenParenToken { get; }
    public RoutePatternToken ArgumentToken { get; }
    public RoutePatternToken CloseParenToken { get; }

    internal override int ChildCount => 3;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => OpenParenToken,
            1 => ArgumentToken,
            2 => CloseParenToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal sealed class RoutePatternPolicyFragment : RoutePatternNode
{
    public RoutePatternPolicyFragment(RoutePatternToken argumentToken)
        : base(RoutePatternKind.PolicyFragment)
    {
        Debug.Assert(argumentToken.Kind == RoutePatternKind.PolicyFragmentToken);
        ArgumentToken = argumentToken;
    }

    public RoutePatternToken ArgumentToken { get; }

    internal override int ChildCount => 1;

    internal override RoutePatternNodeOrToken ChildAt(int index)
        => index switch
        {
            0 => ArgumentToken,
            _ => throw new InvalidOperationException(),
        };

    public override void Accept(IRoutePatternNodeVisitor visitor)
        => visitor.Visit(this);
}

internal abstract class RoutePatternRootPartNode : RoutePatternNode
{
    protected RoutePatternRootPartNode(RoutePatternKind kind)
        : base(kind)
    {
    }
}

internal abstract class RoutePatternSegmentPartNode : RoutePatternNode
{
    protected RoutePatternSegmentPartNode(RoutePatternKind kind)
        : base(kind)
    {
    }
}

internal abstract class RoutePatternParameterPartNode : RoutePatternNode
{
    protected RoutePatternParameterPartNode(RoutePatternKind kind)
        : base(kind)
    {
    }
}
