// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Routing
{
    /// <summary>
    /// Functionality supporting route templates for attribute routes.
    /// </summary>
    public static class AttributeRouteTemplate
    {
        /// <summary>
        /// Combines attribute routing templates.
        /// </summary>
        /// <param name="left">The left template.</param>
        /// <param name="right">The right template.</param>
        /// <returns>A combined template.</returns>
        public static string Combine(string left, string right)
        {
            var result = CombineCore(left, right);
            return CleanTemplate(result);
        }

        private static string CombineCore(string left, string right)
        {
            if (left == null && right == null)
            {
                return null;
            }
            else if (left == null)
            {
                return right;
            }
            else if (right == null)
            {
                return left;
            }

            if (right.StartsWith("~/", StringComparison.OrdinalIgnoreCase) ||
                right.StartsWith("/", StringComparison.OrdinalIgnoreCase) ||
                left.Equals("~/", StringComparison.OrdinalIgnoreCase) ||
                left.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                return right;
            }

            if (left.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return left + right;
            }

            // Both templates contain some text.
            return left + '/' + right;
        }

        private static string CleanTemplate(string result)
        {
            if (result == null)
            {
                return null;
            }

            // This is an invalid combined template, so we don't want to
            // accidentally clean it and produce a valid template. For that
            // reason we ignore the clean up process for it.
            if (result.Equals("//", StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            var startIndex = 0;
            if (result.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }
            else if (result.StartsWith("~/", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 2;
            }

            // We are in the case where the string is "/" or "~/"
            if (startIndex == result.Length)
            {
                return "";
            }

            var subStringLength = result.Length - startIndex;
            if (result.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                subStringLength--;
            }

            return result.Substring(startIndex, subStringLength);
        }

        public static string ReplaceTokens(string template, IDictionary<string, object> values)
        {
            var builder = new StringBuilder();
            var state = TemplateParserState.Plaintext;

            int? tokenStart = null;

            // We'll run the loop one extra time with 'null' to detect the end of the string.
            for (var i = 0; i <= template.Length; i++)
            {
                var c = i < template.Length ? (char?)template[i] : null;
                switch (state)
                {
                    case TemplateParserState.Plaintext:
                        if (c == '[')
                        {
                            state = TemplateParserState.SeenLeft;
                            break;
                        }
                        else if (c == ']')
                        {
                            state = TemplateParserState.SeenRight;
                            break;
                        }
                        else if (c == null)
                        {
                            // We're at the end of the string, nothing left to do.
                            break;
                        }
                        else
                        {
                            builder.Append(c);
                            break;
                        }
                    case TemplateParserState.SeenLeft:
                        if (c == '[')
                        {
                            // This is an escaped left-bracket
                            builder.Append(c);
                            state = TemplateParserState.Plaintext;
                            break;
                        }
                        else if (c == ']')
                        {
                            // This is zero-width parameter - not allowed.
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template,
                                Resources.AttributeRoute_TokenReplacement_EmptyTokenNotAllowed);
                            throw new InvalidOperationException(message);
                        }
                        else if (c == null)
                        {
                            // This is a left-bracket at the end of the string.
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template,
                                Resources.AttributeRoute_TokenReplacement_UnclosedToken);
                            throw new InvalidOperationException(message);
                        }
                        else
                        {
                            tokenStart = i;
                            state = TemplateParserState.InsideToken;
                            break;
                        }
                    case TemplateParserState.SeenRight:
                        if (c == ']')
                        {
                            // This is an escaped right-bracket
                            builder.Append(c);
                            state = TemplateParserState.Plaintext;
                            break;
                        }
                        else if (c == null)
                        {
                            // This is an imbalanced right-bracket at the end of the string.
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template,
                                Resources.AttributeRoute_TokenReplacement_ImbalancedSquareBrackets);
                            throw new InvalidOperationException(message);
                        }
                        else
                        {
                            // This is an imbalanced right-bracket.
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template,
                                Resources.AttributeRoute_TokenReplacement_ImbalancedSquareBrackets);
                            throw new InvalidOperationException(message);
                        }
                    case TemplateParserState.InsideToken:
                        if (c == '[')
                        {
                            state = TemplateParserState.InsideToken | TemplateParserState.SeenLeft;
                            break;
                        }
                        else if (c == ']')
                        {
                            state = TemplateParserState.InsideToken | TemplateParserState.SeenRight;
                            break;
                        }
                        else if (c == null)
                        {
                            // This is an unclosed replacement token
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template, 
                                Resources.AttributeRoute_TokenReplacement_UnclosedToken);
                            throw new InvalidOperationException(message);
                        }
                        else
                        {
                            // This is a just part of the parameter
                            break;
                        }
                    case TemplateParserState.InsideToken | TemplateParserState.SeenLeft:
                        if (c == '[')
                        {
                            // This is an escaped left-bracket
                            state = TemplateParserState.InsideToken;
                            break;
                        }
                        else
                        {
                            // Unescaped left-bracket is not allowed inside a token.
                            var message = Resources.FormatAttributeRoute_TokenReplacement_InvalidSyntax(
                                template, 
                                Resources.AttributeRoute_TokenReplacement_UnescapedBraceInToken);
                            throw new InvalidOperationException(message);
                        }
                    case TemplateParserState.InsideToken | TemplateParserState.SeenRight:
                        if (c == ']')
                        {
                            // This is an escaped right-bracket
                            state = TemplateParserState.InsideToken;
                            break;
                        }
                        else
                        {
                            // This is the end of a replacement token.
                            var token = template
                                .Substring(tokenStart.Value, i - tokenStart.Value - 1)
                                .Replace("[[", "[")
                                .Replace("]]", "]");

                            object value;
                            if (!values.TryGetValue(token, out value))
                            {
                                // Value not found
                                var message = Resources.FormatAttributeRoute_TokenReplacement_ReplacementValueNotFound(
                                    template,
                                    token,
                                    string.Join(", ", values.Keys));
                                throw new InvalidOperationException(message);
                            }

                            builder.Append(value);
                            
                            if (c == '[')
                            {
                                state = TemplateParserState.SeenLeft;
                            }
                            else if (c == ']')
                            {
                                state = TemplateParserState.SeenRight;
                            }
                            else if (c == null)
                            {
                                state = TemplateParserState.Plaintext;
                            }
                            else
                            {
                                builder.Append(c);
                                state = TemplateParserState.Plaintext;
                            }

                            tokenStart = null;
                            break;
                        }
                }
            }

            return builder.ToString();
        }

        [Flags]
        private enum TemplateParserState : uint
        {
            // default state - allow non-special characters to pass through to the 
            // buffer.
            Plaintext = 0,

            // We're inside a replacement token, may be combined with other states to detect
            // a possible escaped bracket inside the token.
            InsideToken = 1,

            // We've seen a left brace, need to see the next character to find out if it's escaped
            // or not.
            SeenLeft = 2,

            // We've seen a right brace, need to see the next character to find out if it's escaped
            // or not.
            SeenRight = 4,
        }
    }
}