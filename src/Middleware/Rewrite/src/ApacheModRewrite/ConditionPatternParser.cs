// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    /// <summary>
    /// Parses the "CondPattern" portion of the RewriteCond. 
    /// RewriteCond TestString CondPattern
    /// </summary>
    internal class ConditionPatternParser
    {
        private const char Not = '!';
        private const char Dash = '-';
        private const char Less = '<';
        private const char Greater = '>';
        private const char EqualSign = '=';

        /// <summary>
        /// Given a CondPattern, create a ParsedConditionExpression, containing the type of operation
        /// and value.
        /// ParsedConditionExpression is an intermediary object, which will be made into a ConditionExpression
        /// once the flags are parsed.
        /// </summary>
        /// <param name="condition">The CondPattern portion of a mod_rewrite RewriteCond.</param>
        /// <returns>A new parsed condition.</returns>
        public ParsedModRewriteInput ParseActionCondition(string condition)
        {
            if (condition == null)
            {
                condition = string.Empty;
            }
            var context = new ParserContext(condition);
            var results = new ParsedModRewriteInput();
            if (!context.Next())
            {
                throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
            }

            // If we hit a !, invert the condition
            if (context.Current == Not)
            {
                results.Invert = true;
                if (!context.Next())
                {
                    // Dangling !
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                }
            }

            // Control Block for strings. Set the operation and type fields based on the sign 
            // Switch on current character
            switch (context.Current)
            {
                case Greater:
                    if (!context.Next())
                    {
                        // Dangling ">"
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                    }
                    if (context.Current == EqualSign)
                    {
                        if (!context.Next())
                        {
                            // Dangling ">="
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                        }
                        results.OperationType = OperationType.GreaterEqual;
                        results.ConditionType = ConditionType.StringComp;
                    }
                    else
                    {
                        results.OperationType = OperationType.Greater;
                        results.ConditionType = ConditionType.StringComp;
                    }
                    break;
                case Less:
                    if (!context.Next())
                    {
                        // Dangling "<"
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                    }
                    if (context.Current == EqualSign)
                    {
                        if (!context.Next())
                        {
                            // Dangling "<="
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                        }
                        results.OperationType = OperationType.LessEqual;
                        results.ConditionType = ConditionType.StringComp;
                    }
                    else
                    {
                        results.OperationType = OperationType.Less;
                        results.ConditionType = ConditionType.StringComp;
                    }
                    break;
                case EqualSign:
                    if (!context.Next())
                    {
                        // Dangling "="
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
                    }
                    results.OperationType = OperationType.Equal;
                    results.ConditionType = ConditionType.StringComp;
                    break;
                case Dash:
                    results = ParseProperty(context, results.Invert);
                    if (results.ConditionType == ConditionType.PropertyTest)
                    {
                        return results;
                    }
                    context.Next();
                    break;
                default:
                    results.ConditionType = ConditionType.Regex;
                    break;
            }

            // Capture the rest of the string guarantee validity.
            results.Operand = condition.Substring(context.GetIndex());
            if (IsValidActionCondition(results))
            {
                return results;
            }
            else
            {
                throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(condition, context.Index));
            }
        }

        /// <summary>
        /// Given that the current index is a property (ex checks for directory or regular files), create a
        /// new ParsedConditionExpression with the appropriate property operation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invert"></param>
        /// <returns></returns>
        private static ParsedModRewriteInput ParseProperty(ParserContext context, bool invert)
        {
            if (!context.Next())
            {
                throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
            }

            switch (context.Current)
            {
                case 'd':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.Directory, operand: null);
                case 'f':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.RegularFile, operand: null);
                case 'F':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.ExistingFile, operand: null);
                case 'h':
                case 'L':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.SymbolicLink, operand: null);
                case 's':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.Size, operand: null);
                case 'U':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.ExistingUrl, operand: null);
                case 'x':
                    return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.Executable, operand: null);
                case 'e':
                    if (!context.Next() || context.Current != 'q')
                    {
                        // Illegal statement.
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
                    }
                    return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.Equal, operand: null);
                case 'g':
                    if (!context.Next())
                    {
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
                    }
                    switch (context.Current)
                    {
                        case 't':
                            return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.Greater, operand: null);
                        case 'e':
                            return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.GreaterEqual, operand: null);
                        default:
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
                    }
                case 'l':
                    // name conflict with -l and -lt/-le, so the assumption is if there is no 
                    // charcters after -l, we assume it a symbolic link
                    if (!context.Next())
                    {
                        return new ParsedModRewriteInput(invert, ConditionType.PropertyTest, OperationType.SymbolicLink, operand: null);
                    }
                    switch (context.Current)
                    {
                        case 't':
                            return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.Less, operand: null);
                        case 'e':
                            return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.LessEqual, operand: null);
                        default:
                            throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
                    }
                case 'n':
                    if (!context.Next() || context.Current != 'e')
                    {
                        throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
                    }
                    return new ParsedModRewriteInput(invert, ConditionType.IntComp, OperationType.NotEqual, operand: null);
                default:
                    throw new FormatException(Resources.FormatError_InputParserUnrecognizedParameter(context.Template, context.Index));
            }
        }

        private static bool IsValidActionCondition(ParsedModRewriteInput results)
        {
            if (results.ConditionType == ConditionType.IntComp)
            {
                // If the type is an integer, verify operand is actually an int
                int res;
                if (!int.TryParse(results.Operand, NumberStyles.None, CultureInfo.InvariantCulture, out res))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
