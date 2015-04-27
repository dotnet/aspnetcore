// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class ImplicitExpressionEditHandlerTest
    {
        public static TheoryData<ImplicitExpressionEditHandler, ImplicitExpressionEditHandler> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<ImplicitExpressionEditHandler, ImplicitExpressionEditHandler>
                {
                    {
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string>(),
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string>(),
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Tokenizer not involved in equality check or hash code calculation.
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string> { "keyword 1" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 1" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        }
                    },
                    {
                        // Only comparers are different (HashSet's comparer does not affect GetHashCode of entries).
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 2", "keyword 3" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "keyword 2", "keyword 3" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        }
                    },
                };
            }
        }

        public static TheoryData<ImplicitExpressionEditHandler, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<ImplicitExpressionEditHandler, object>
                {
                    {
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 4" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.WhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        null
                    },
                    {
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 4" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.WhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new object()
                    },
                    {
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 4" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.WhiteSpace,
                            EditorHints = EditorHints.None,
                        },
                        new SpanEditHandler( tokenizer: _ => null)
                    },
                    {
                        // Different AcceptedCharacters.
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 5" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 5" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Different AcceptTrailingDot.
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 6" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 6" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Different Keywords.
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 7" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.NewLine,
                            EditorHints = EditorHints.None,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 8" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.NewLine,
                            EditorHints = EditorHints.None,
                        }
                    },
                    {
                        // Different Keywords comparers (Equals uses left comparer).
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string> { "keyword 9" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.None,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => null,
                            keywords: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "KEYWORD 9" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.None,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Different Keywords (count).
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 10" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.NonWhiteSpace,
                            EditorHints = EditorHints.None,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 10", "keyword 11" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.NonWhiteSpace,
                            EditorHints = EditorHints.None,
                        }
                    },
                    {
                        // Different Keywords (count).
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string> { "keyword 12" },
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.WhiteSpace,
                            EditorHints = EditorHints.None,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string>(),
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.WhiteSpace,
                            EditorHints = EditorHints.None,
                        }
                    },
                    {
                        // Different EditorHints.
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 13" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            keywords: new HashSet<string> { "keyword 13" },
                            acceptTrailingDot: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.None,
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(
            ImplicitExpressionEditHandler leftObject,
            ImplicitExpressionEditHandler rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(ImplicitExpressionEditHandler leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            ImplicitExpressionEditHandler leftObject,
            ImplicitExpressionEditHandler rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
