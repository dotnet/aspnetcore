// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;

internal enum RoutePatternKind
{
    None = 0,
    EndOfFile,
    Segment,
    CompilationUnit,
    Separator,
    Literal,
    Replacement,
    Parameter,
    CatchAll,
    ParameterName,
    Optional,
    DefaultValue,
    PolicyFragment,
    PolicyFragmentEscaped,
    ParameterPolicy,

    // Tokens
    TextToken,
    SlashToken,
    TildeToken,
    /// <summary>
    /// {
    /// </summary>
    OpenBraceToken,
    /// <summary>
    /// }
    /// </summary>
    CloseBraceToken,
    /// <summary>
    /// [
    /// </summary>
    OpenBracketToken,
    /// <summary>
    /// ]
    /// </summary>
    CloseBracketToken,
    DotToken,
    EqualsToken,
    ColonToken,
    ReplacementToken,
    AsteriskToken,
    /// <summary>
    /// (
    /// </summary>
    OpenParenToken,
    /// <summary>
    /// )
    /// </summary>
    CloseParenToken,
    QuestionMarkToken,
    CommaToken,
    ParameterNameToken,
    DefaultValueToken,
    PolicyNameToken,
    PolicyFragmentToken,
}
