// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis.Classification;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification;

public static partial class FormattedClassifications
{
    public static class Regex
    {
        [DebuggerStepThrough]
        public static FormattedClassification Anchor(string value) => New(value, ClassificationTypeNames.RegexAnchor);

        [DebuggerStepThrough]
        public static FormattedClassification Grouping(string value) => New(value, ClassificationTypeNames.RegexGrouping);

        [DebuggerStepThrough]
        public static FormattedClassification OtherEscape(string value) => New(value, ClassificationTypeNames.RegexOtherEscape);

        [DebuggerStepThrough]
        public static FormattedClassification SelfEscapedCharacter(string value) => New(value, ClassificationTypeNames.RegexSelfEscapedCharacter);

        [DebuggerStepThrough]
        public static FormattedClassification Alternation(string value) => New(value, ClassificationTypeNames.RegexAlternation);

        [DebuggerStepThrough]
        public static FormattedClassification CharacterClass(string value) => New(value, ClassificationTypeNames.RegexCharacterClass);

        [DebuggerStepThrough]
        public static FormattedClassification Text(string value) => New(value, ClassificationTypeNames.RegexText);

        [DebuggerStepThrough]
        public static FormattedClassification Quantifier(string value) => New(value, ClassificationTypeNames.RegexQuantifier);

        [DebuggerStepThrough]
        public static FormattedClassification Comment(string value) => New(value, ClassificationTypeNames.RegexComment);
    }
}
