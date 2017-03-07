// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.CodeAnalysis.Razor
{
    // Internal for testing
    internal static class RequiredAttributeParser
    {
        public static void AddRequiredAttributes(string requiredAttributes, TagMatchingRuleBuilder ruleBuilder)
        {
            var requiredAttributeParser = new DefaultRequiredAttributeParser(requiredAttributes);
            requiredAttributeParser.AddRequiredAttributes(ruleBuilder);
        }

        private class DefaultRequiredAttributeParser
        {
            private const char RequiredAttributeWildcardSuffix = '*';

            private static readonly IReadOnlyDictionary<char, RequiredAttributeDescriptor.ValueComparisonMode> CssValueComparisons =
                new Dictionary<char, RequiredAttributeDescriptor.ValueComparisonMode>
                {
                        { '=', RequiredAttributeDescriptor.ValueComparisonMode.FullMatch },
                        { '^', RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch },
                        { '$', RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch }
                };
            private static readonly char[] InvalidPlainAttributeNameCharacters = { ' ', '\t', ',', RequiredAttributeWildcardSuffix };
            private static readonly char[] InvalidCssAttributeNameCharacters = (new[] { ' ', '\t', ',', ']' })
                .Concat(CssValueComparisons.Keys)
                .ToArray();
            private static readonly char[] InvalidCssQuotelessValueCharacters = { ' ', '\t', ']' };

            private int _index;
            private string _requiredAttributes;

            public DefaultRequiredAttributeParser(string requiredAttributes)
            {
                _requiredAttributes = requiredAttributes;
            }

            private char Current => _requiredAttributes[_index];

            private bool AtEnd => _index >= _requiredAttributes.Length;

            public void AddRequiredAttributes(TagMatchingRuleBuilder ruleBuilder)
            {
                if (string.IsNullOrEmpty(_requiredAttributes))
                {
                    return;
                }
                var descriptors = new List<RequiredAttributeDescriptor>();

                PassOptionalWhitespace();

                do
                {
                    var successfulParse = true;
                    ruleBuilder.RequireAttribute(attributeBuilder =>
                    {
                        if (At('['))
                        {
                            if (!TryParseCssSelector(attributeBuilder))
                            {
                                successfulParse = false;
                                return;
                            }
                        }
                        else
                        {
                            ParsePlainSelector(attributeBuilder);
                        }

                        PassOptionalWhitespace();

                        if (At(','))
                        {
                            _index++;

                            if (!EnsureNotAtEnd(attributeBuilder))
                            {
                                successfulParse = false;
                                return;
                            }
                        }
                        else if (!AtEnd)
                        {
                            var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeCharacter(Current, _requiredAttributes);
                            attributeBuilder.AddDiagnostic(diagnostic);
                            successfulParse = false;
                            return;
                        }

                        PassOptionalWhitespace();
                    });

                    if (!successfulParse)
                    {
                        break;
                    }
                }
                while (!AtEnd);
            }

            private void ParsePlainSelector(RequiredAttributeDescriptorBuilder attributeBuilder)
            {
                var nameEndIndex = _requiredAttributes.IndexOfAny(InvalidPlainAttributeNameCharacters, _index);
                string attributeName;

                var nameComparison = RequiredAttributeDescriptor.NameComparisonMode.FullMatch;
                if (nameEndIndex == -1)
                {
                    attributeName = _requiredAttributes.Substring(_index);
                    _index = _requiredAttributes.Length;
                }
                else
                {
                    attributeName = _requiredAttributes.Substring(_index, nameEndIndex - _index);
                    _index = nameEndIndex;

                    if (_requiredAttributes[nameEndIndex] == RequiredAttributeWildcardSuffix)
                    {
                        nameComparison = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;

                        // Move past wild card
                        _index++;
                    }
                }

                attributeBuilder.Name(attributeName);
                attributeBuilder.NameComparisonMode(nameComparison);
            }

            private void ParseCssAttributeName(RequiredAttributeDescriptorBuilder builder)
            {
                var nameStartIndex = _index;
                var nameEndIndex = _requiredAttributes.IndexOfAny(InvalidCssAttributeNameCharacters, _index);
                nameEndIndex = nameEndIndex == -1 ? _requiredAttributes.Length : nameEndIndex;
                _index = nameEndIndex;

                var attributeName = _requiredAttributes.Substring(nameStartIndex, nameEndIndex - nameStartIndex);

                builder.Name(attributeName);
            }

            private bool TryParseCssValueComparison(RequiredAttributeDescriptorBuilder builder, out RequiredAttributeDescriptor.ValueComparisonMode valueComparison)
            {
                Debug.Assert(!AtEnd);

                if (CssValueComparisons.TryGetValue(Current, out valueComparison))
                {
                    var op = Current;
                    _index++;

                    if (op != '=' && At('='))
                    {
                        // Two length operator (ex: ^=). Move past the second piece
                        _index++;
                    }
                    else if (op != '=') // We're at an incomplete operator (ex: [foo^]
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_PartialRequiredAttributeOperator(op, _requiredAttributes);
                        builder.AddDiagnostic(diagnostic);

                        return false;
                    }
                }
                else if (!At(']'))
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeOperator(Current, _requiredAttributes);
                    builder.AddDiagnostic(diagnostic);

                    return false;
                }

                builder.ValueComparisonMode(valueComparison);

                return true;
            }

            private bool TryParseCssValue(RequiredAttributeDescriptorBuilder builder)
            {
                int valueStart;
                int valueEnd;
                if (At('\'') || At('"'))
                {
                    var quote = Current;

                    // Move past the quote
                    _index++;

                    valueStart = _index;
                    valueEnd = _requiredAttributes.IndexOf(quote, _index);
                    if (valueEnd == -1)
                    {
                        var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeMismatchedQuotes(quote, _requiredAttributes);
                        builder.AddDiagnostic(diagnostic);

                        return false;
                    }
                    _index = valueEnd + 1;
                }
                else
                {
                    valueStart = _index;
                    var valueEndIndex = _requiredAttributes.IndexOfAny(InvalidCssQuotelessValueCharacters, _index);
                    valueEnd = valueEndIndex == -1 ? _requiredAttributes.Length : valueEndIndex;
                    _index = valueEnd;
                }

                var value = _requiredAttributes.Substring(valueStart, valueEnd - valueStart);

                builder.Value(value);

                return true;
            }

            private bool TryParseCssSelector(RequiredAttributeDescriptorBuilder attributeBuilder)
            {
                Debug.Assert(At('['));

                // Move past '['.
                _index++;
                PassOptionalWhitespace();

                ParseCssAttributeName(attributeBuilder);

                PassOptionalWhitespace();

                if (!EnsureNotAtEnd(attributeBuilder))
                {
                    return false;
                }

                if (!TryParseCssValueComparison(attributeBuilder, out RequiredAttributeDescriptor.ValueComparisonMode valueComparison))
                {
                    return false;
                }

                PassOptionalWhitespace();

                if (!EnsureNotAtEnd(attributeBuilder))
                {
                    return false;
                }

                if (valueComparison != RequiredAttributeDescriptor.ValueComparisonMode.None && !TryParseCssValue(attributeBuilder))
                {
                    return false;
                }

                PassOptionalWhitespace();

                if (At(']'))
                {
                    // Move past the ending bracket.
                    _index++;
                    return true;
                }
                else if (AtEnd)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace(_requiredAttributes);
                    attributeBuilder.AddDiagnostic(diagnostic);
                }
                else
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_InvalidRequiredAttributeCharacter(Current, _requiredAttributes);
                    attributeBuilder.AddDiagnostic(diagnostic);
                }

                return false;
            }

            private bool EnsureNotAtEnd(RequiredAttributeDescriptorBuilder builder)
            {
                if (AtEnd)
                {
                    var diagnostic = RazorDiagnosticFactory.CreateTagHelper_CouldNotFindMatchingEndBrace(_requiredAttributes);
                    builder.AddDiagnostic(diagnostic);

                    return false;
                }

                return true;
            }

            private bool At(char c)
            {
                return !AtEnd && Current == c;
            }

            private void PassOptionalWhitespace()
            {
                while (!AtEnd && (Current == ' ' || Current == '\t'))
                {
                    _index++;
                }
            }
        }
    }
}
