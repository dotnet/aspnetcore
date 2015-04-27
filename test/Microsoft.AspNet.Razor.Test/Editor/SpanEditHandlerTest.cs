// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Editor;
using Microsoft.AspNet.Razor.Tokenizer.Symbols;
using Xunit;

namespace Microsoft.AspNet.Razor.Parser.SyntaxTree
{
    public class SpanEditHandlerTest
    {
        public static TheoryData<SpanEditHandler, SpanEditHandler> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<SpanEditHandler, SpanEditHandler>
                {
                    {
                        new SpanEditHandler(tokenizer: null)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new SpanEditHandler(tokenizer: null)
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Tokenizer not involved in equality check or hash code calculation.
                        new SpanEditHandler(tokenizer: null)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        },
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        }
                    },
                };
            }
        }

        public static TheoryData<SpanEditHandler, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<SpanEditHandler, object>
                {
                    {
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        null
                    },
                    {
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new object()
                    },
                    {
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            EditorHints = EditorHints.None,
                        },
                        new AutoCompleteEditHandler(
                            tokenizer: _ => Enumerable.Empty<ISymbol>(),
                            autoCompleteAtEndOfSpan: true)
                        {
                            AcceptedCharacters = AcceptedCharacters.Any,
                            AutoCompleteString = "two string",
                            EditorHints = EditorHints.None,
                        }
                    },
                    {
                        new SpanEditHandler(tokenizer: null)
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new ImplicitExpressionEditHandler(
                            tokenizer: null,
                            keywords: new HashSet<string>(),
                            acceptTrailingDot: false)
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Different AcceptedCharacters.
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.AllWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.AnyExceptNewline,
                            EditorHints = EditorHints.VirtualPath,
                        }
                    },
                    {
                        // Different EditorHints.
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.NonWhiteSpace,
                            EditorHints = EditorHints.VirtualPath,
                        },
                        new SpanEditHandler(tokenizer: _ => Enumerable.Empty<ISymbol>())
                        {
                            AcceptedCharacters = AcceptedCharacters.NonWhiteSpace,
                            EditorHints = EditorHints.None,
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(SpanEditHandler leftObject, SpanEditHandler rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(SpanEditHandler leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(SpanEditHandler leftObject, SpanEditHandler rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
