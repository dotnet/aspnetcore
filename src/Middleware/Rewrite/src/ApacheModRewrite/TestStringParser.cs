// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.PatternSegments;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    /// <summary>
    /// Parses the TestString segment of the mod_rewrite condition.
    /// </summary>
    internal class TestStringParser
    {
        private const char Percent = '%';
        private const char Dollar = '$';
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
        /// http://httpd.apache.org/docs/current/mod/mod_rewrite.html
        public Pattern Parse(string testString)
        {
            if (testString == null)
            {
                testString = string.Empty;
            }
            var context = new ParserContext(testString);
            var results = new List<PatternSegment>();
            while (context.Next())
            {
                switch (context.Current)
                {
                    case Percent:
                        // This is a server parameter, parse for a condition variable
                        if (!context.Next())
                        {
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(testString, context.Index));
                        }
                        ParseConditionParameter(context, results);
                        break;
                    case Dollar:
                        // This is a parameter from the rule, verify that it is a number from 0 to 9 directly after it
                        // and create a new Pattern Segment.
                        if (!context.Next())
                        {
                            throw new FormatException(Resources.FormatError_InputParserNoBackreference(context.Index));
                        }
                        context.Mark();
                        if (context.Current >= '0' && context.Current <= '9')
                        {
                            context.Next();
                            var ruleVariable = context.Capture();
                            context.Back();
                            var parsedIndex = int.Parse(ruleVariable);

                            results.Add(new RuleMatchSegment(parsedIndex));
                        }
                        else
                        {
                            throw new FormatException(Resources.FormatError_InputParserInvalidInteger(testString, context.Index));
                        }
                        break;
                    default:
                        ParseLiteral(context, results);
                        break;
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
        private static void ParseConditionParameter(ParserContext context, IList<PatternSegment> results)
        {
            // Parse { }
            if (context.Current == OpenBrace)
            {
                // Start of a server variable
                if (!context.Next())
                {
                    // Dangling {
                    throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                }
                context.Mark();
                while (context.Current != CloseBrace)
                {
                    if (!context.Next())
                    {
                        throw new FormatException(Resources.FormatError_InputParserMissingCloseBrace(context.Index));
                    }
                    else if (context.Current == Colon)
                    {
                        // Have a segmented look up Ex: HTTP:xxxx 
                        // Most of these we can't handle
                        throw new NotImplementedException("Segmented Lookups no implemented");
                    }
                }

                // Need to verify server variable captured exists
                var rawServerVariable = context.Capture();
                results.Add(ServerVariables.FindServerVariable(rawServerVariable, context));
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
                var parsedIndex = int.Parse(rawConditionParameter);
                results.Add(new ConditionMatchSegment(parsedIndex));
            }
            else
            {
                // illegal escape of a character
                throw new FormatException(Resources.FormatError_InputParserInvalidInteger(context.Template, context.Index));
            }
        }

        /// <summary>
        /// Parse a string literal in the test string. Continues capturing until the start of a new variable type.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private static void ParseLiteral(ParserContext context, IList<PatternSegment> results)
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
            // add results
            results.Add(new LiteralSegment(literal));
        }
    }
}
