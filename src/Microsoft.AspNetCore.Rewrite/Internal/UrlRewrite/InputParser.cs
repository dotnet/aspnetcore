// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    /// <summary>
    /// </summary>
    public class InputParser
    {
        private const char Colon = ':';
        private const char OpenBrace = '{';
        private const char CloseBrace = '}';

        /// <summary>
        /// Creates a pattern, which is a template to create a new test string to 
        /// compare to the condition. Can contain server variables, back references, etc.
        /// </summary>
        /// <param name="testString"></param>
        /// <returns>A new <see cref="Pattern"/>, containing a list of <see cref="PatternSegment"/></returns>
        public static Pattern ParseInputString(string testString)
        {
            if (testString == null)
            {
                testString = string.Empty;
            }

            var context = new ParserContext(testString);
            return ParseString(context);
        }

        private static Pattern ParseString(ParserContext context)
        {
            var results = new List<PatternSegment>();
            while (context.Next())
            {
                if (context.Current == OpenBrace)
                {
                    // This is a server parameter, parse for a condition variable
                    if (!context.Next())
                    {
                        // missing {
                        throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                    }
                    ParseParameter(context, results);
                }
                else if (context.Current == CloseBrace)
                {
                    // TODO we should be throwing a syntax error if we have uneven close braces
                    // Can fix by keeping track of the number of '{' and '}' with an int, where {
                    // increments and } decrements. Throw if < 0.
                    return new Pattern(results);
                }
                else
                {
                    // Parse for literals, which will return on either the end of the test string 
                    // or when it hits a special character
                    ParseLiteral(context, results);
                }
            }
            return new Pattern(results);
        }

        private static void ParseParameter(ParserContext context, List<PatternSegment> results)
        {
            context.Mark();
            // Four main cases:
            // 1. {NAME} - Server Variable, create lambda to get the part of the context
            // 2. {R:1}  - Rule parameter
            // 3. {C:1}  - Condition Parameter
            // 4. {function:xxx} - String function 
            // TODO consider perf here. This is on startup and will only happen one time
            // (unless we support Reload)
            string parameter;
            while (context.Next())
            {
                if (context.Current == CloseBrace)
                {
                    // This is just a server variable, so we do a lookup and verify the server variable exists.
                    parameter = context.Capture();
                    results.Add(ServerVariables.FindServerVariable(parameter));
                    return;
                }
                else if (context.Current == Colon)
                {
                    parameter = context.Capture();
                    
                    // Only 5 strings to expect here. Case sensitive.
                    switch (parameter)
                    {
                        case "ToLower":
                            {
                                var pattern = ParseString(context);
                                results.Add(new ToLowerSegment(pattern));

                                // at this point, we expect our context to be on the ending closing brace,
                                // because the ParseString() call will increment the context until it 
                                // has processed the new string.
                                if (context.Current != CloseBrace)
                                {
                                    throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                                }
                                return;
                            }
                        case "UrlDecode":
                            {
                                throw new NotImplementedException("UrlDecode is not supported.");
                            }
                        case "UrlEncode":
                            {
                                var pattern = ParseString(context);
                                results.Add(new UrlEncodeSegment(pattern));

                                if (context.Current != CloseBrace)
                                {
                                    throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                                }
                                return;
                            }
                        case "R":
                            {
                                var index = GetBackReferenceIndex(context);
                                results.Add(new RuleMatchSegment(index));
                                return;
                            }
                        case "C":
                            {
                                var index = GetBackReferenceIndex(context);
                                results.Add(new ConditionMatchSegment(index));
                                return;
                            }
                        default:
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(parameter, context.Index));
                    }
                }
            }
            throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
        }

        private static int GetBackReferenceIndex(ParserContext context)
        {
            if (!context.Next())
            {
                throw new FormatException(Resources.FormatError_InputParserNoBackreference(context.Index));
            }

            context.Mark();
            while (context.Current != CloseBrace)
            {
                if (!context.Next())
                {
                    throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                }
            }

            var res = context.Capture();
            int index;
            if (!int.TryParse(res, out index))
            {
                throw new FormatException(Resources.FormatError_InputParserInvalidInteger(res, context.Index));
            }

            if (index > 9 || index < 0)
            {
                throw new FormatException(Resources.FormatError_InputParserIndexOutOfRange(res, context.Index));
            }
            return index;
        }

        private static bool ParseLiteral(ParserContext context, List<PatternSegment> results)
        {
            context.Mark();
            string literal;
            while (true)
            {
                if (context.Current == OpenBrace || context.Current == CloseBrace)
                {
                    literal = context.Capture();
                    context.Back();
                    break;
                }

                if (!context.Next())
                {
                    literal = context.Capture();
                    break;
                }
            }

            results.Add(new LiteralSegment(literal));
            return true;
        }
    }
}
