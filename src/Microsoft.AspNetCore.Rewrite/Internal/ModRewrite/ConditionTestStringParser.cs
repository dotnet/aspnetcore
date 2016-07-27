// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// Parses the TestString segment of the mod_rewrite condition.
    /// </summary>
    public class ConditionTestStringParser
    {
        private const char Percent = '%';
        private const char Dollar = '$';
        private const char Space = ' ';
        private const char Colon = ':';
        private const char OpenBrace = '{';
        private const char CloseBrace = '}';

        /// <summary>
        /// Creates a pattern, which is a template to create a new test string to 
        /// compare to the condition pattern. Can contain server variables, back references, etc.
        /// </summary>
        /// <param name="testString">The test string portion of the RewriteCond
        /// Examples:
        /// %{REMOTE_ADDR}
        /// /var/www/%{REQUEST_URI}
        /// %1
        /// $1</param>
        /// <returns>A new <see cref="Pattern"/>, containing a list of <see cref="PatternSegment"/></returns>
        public static Pattern ParseConditionTestString(string testString)
        {
            if (testString == null)
            {
                testString = string.Empty;
            }
            var context = new ParserContext(testString);
            var results = new List<PatternSegment>();
            while (context.Next())
            {
                if (context.Current == Percent)
                {
                    // This is a server parameter, parse for a condition variable
                    if (!context.Next())
                    {
                        throw new FormatException(context.Error());
                    }
                    if (!ParseConditionParameter(context, results))
                    {
                        throw new FormatException(context.Error());
                    }
                }
                else if (context.Current == Dollar)
                {
                    // This is a parameter from the rule, verify that it is a number from 0 to 9 directly after it
                    // and create a new Pattern Segment.
                    if (!context.Next())
                    {
                        throw new FormatException(context.Error());
                    }
                    context.Mark();
                    if (context.Current >= '0' && context.Current <= '9')
                    {
                        context.Next();
                        var ruleVariable = context.Capture();
                        context.Back();
                        results.Add(new PatternSegment(ruleVariable, SegmentType.RuleParameter));
                    }
                    else
                    {
                        throw new FormatException(context.Error());
                    }
                }
                else
                {
                    // Parse for literals, which will return on either the end of the test string 
                    // or when it hits a special character
                    if (!ParseLiteral(context, results))
                    {
                        throw new FormatException(context.Error());
                    }
                }
            }
            return new Pattern(results);
        }

        /// <summary>
        /// Obtains the condition parameter, which could either be a condition variable or a 
        /// server variable. Assumes the current character is immediately after the '%'.
        /// context, on return will be on the last character of variable captured, such that after 
        /// Next() is called, it will be on the character immediately after the condition parameter.
        /// </summary>
        /// <param name="context">The ParserContext</param>
        /// <param name="results">The List of results which the new condition parameter will be added.</param>
        /// <returns>true </returns>
        private static bool ParseConditionParameter(ParserContext context, List<PatternSegment> results)
        {
            // Parse { }
            if (context.Current == OpenBrace)
            {
                // Start of a server variable
                if (!context.Next())
                {
                    // Dangling {
                    return false;
                }
                context.Mark();
                while (context.Current != CloseBrace)
                {
                    if (!context.Next())
                    {
                        // No closing } for the server variable
                        return false;
                    }
                    else if (context.Current == Colon)
                    {
                        // Have a segmented look up Ex: HTTP:xxxx 
                        // TODO 
                    }
                }

                // Need to verify server variable captured exists
                var rawServerVariable = context.Capture();
                if (IsValidServerVariable(rawServerVariable))
                {
                    results.Add(new PatternSegment(rawServerVariable, SegmentType.ServerParameter));
                }
                else
                {
                    // invalid.
                    return false;
                }
            }
            else if (context.Current >= '0' && context.Current <= '9')
            {
                // means we have a segmented lookup
                // store information in the testString result to know what to look up.
                context.Mark();
                context.Next();
                var rawConditionParameter = context.Capture();
                // Once we leave this method, the while loop will call next again. Because
                // capture is exclusive, we need to go one past the end index, capture, and then go back.
                context.Back();
                results.Add(new PatternSegment(rawConditionParameter, SegmentType.ConditionParameter));
            }
            else
            {
                // illegal escape of a character
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parse a string literal in the test string. Continues capturing until the start of a new variable type.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private static bool ParseLiteral(ParserContext context, List<PatternSegment> results)
        {
            context.Mark();
            string literal;
            while (true)
            {
                if (context.Current == Percent || context.Current == Dollar)
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

            if (IsValidLiteral(context, literal))
            {
                // add results
                results.Add(new PatternSegment(literal, SegmentType.Literal));
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsValidLiteral(ParserContext context, string literal)
        {
            // TODO Once escape characters are discussed, figure this out.
            return true;
        }

        private static bool IsValidServerVariable(string variable)
        {
            // TODO Once escape characters are discussed, figure this out.
            return ServerVariables.ValidServerVariables.Contains(variable);
        }
    }
}
