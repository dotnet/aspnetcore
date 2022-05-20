// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;

internal enum RoutePatternKind
{
    None = 0,
    EndOfFile,
    Segment,
    CompilationUnit,
    Seperator,
    Literal,
    Replacement,

    // Tokens
    TextToken,
    SlashToken,
    NumberToken,
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
    Parameter,
    ParameterName,
    ParameterNameToken,
    CatchAll,
    ParameterPolicy,
    DefaultValueToken,
    PolicyNameToken,
    PolicyFragmentToken,
    Optional,
    DefaultValue,
    PolicyFragment,
    PolicyFragmentEscaped,
}
