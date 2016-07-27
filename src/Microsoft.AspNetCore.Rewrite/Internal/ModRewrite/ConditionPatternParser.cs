// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// Parses the "CondPattern" portion of the RewriteCond. 
    /// RewriteCond TestString CondPattern
    /// </summary>
    public static class ConditionPatternParser
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
        public static ParsedModRewriteExpression ParseActionCondition(string condition)
        {
            if (condition == null)
            {
                condition = string.Empty;
            }
            var context = new ParserContext(condition);
            var results = new ParsedModRewriteExpression();
            if (!context.Next())
            {
                throw new FormatException(context.Error());
            }

            // If we hit a !, make sure the condition is inverted when resolving the string
            if (context.Current == Not)
            {
                results.Invert = true;
                if (!context.Next())
                {
                    throw new FormatException(context.Error());
                }
            }

            // Control Block for strings. Set the operation and type fields based on the sign 
            switch (context.Current)
            {
                case Greater:
                    if (!context.Next())
                    {
                        // Dangling ">"
                        throw new FormatException(context.Error());
                    }
                    if (context.Current == EqualSign)
                    {
                        if (!context.Next())
                        {
                            // Dangling ">="
                            throw new FormatException(context.Error());
                        }
                        results.Operation = OperationType.GreaterEqual;
                        results.Type = ConditionType.StringComp;
                    }
                    else
                    {
                        results.Operation = OperationType.Greater;
                        results.Type = ConditionType.StringComp;
                    }
                    break;
                case Less:
                    if (!context.Next())
                    {
                        // Dangling "<"
                        throw new FormatException(context.Error());
                    }
                    if (context.Current == EqualSign)
                    {
                        if (!context.Next())
                        {
                            // Dangling "<="
                            throw new FormatException(context.Error());
                        }
                        results.Operation = OperationType.LessEqual;
                        results.Type = ConditionType.StringComp;
                    }
                    else
                    {
                        results.Operation = OperationType.Less;
                        results.Type = ConditionType.StringComp;
                    }
                    break;
                case EqualSign:
                    if (!context.Next())
                    {
                        // Dangling "="
                        throw new FormatException(context.Error());
                    }
                    results.Operation = OperationType.Equal;
                    results.Type = ConditionType.StringComp;
                    break;
                case Dash:
                    results = ParseProperty(context, results.Invert);
                    if (results.Type == ConditionType.PropertyTest)
                    {
                        return results;
                    }
                    context.Next();
                    break;
                default:
                    results.Type = ConditionType.Regex;
                    break;
            }

            // Capture the rest of the string guarantee validity.
            results.Operand = (condition.Substring(context.GetIndex()));
            if (IsValidActionCondition(results))
            {
                return results;
            }
            else
            {
                throw new FormatException(context.Error());
            }
        }

        /// <summary>
        /// Given that the current index is a property (ex checks for directory or regular files), create a
        /// new ParsedConditionExpression with the appropriate property operation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="invert"></param>
        /// <returns></returns>
        public static ParsedModRewriteExpression ParseProperty(ParserContext context, bool invert)
        {
            if (!context.Next())
            {
                throw new FormatException(context.Error());
            }
            switch (context.Current)
            {
                case 'd':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.Directory, null);
                case 'f':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.RegularFile, null);
                case 'F':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.ExistingFile, null);
                case 'h':
                case 'L':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.SymbolicLink, null);
                case 's':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.Size, null);
                case 'U':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.ExistingUrl, null);
                case 'x':
                    return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.Executable, null);
                case 'e':

                    if (!context.Next() || context.Current != 'q')
                    {
                        // Illegal statement.
                        throw new FormatException(context.Error());
                    }
                    return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.Equal, null);
                case 'g':
                    if (!context.Next())
                    {
                        throw new FormatException(context.Error());
                    }
                    if (context.Current == 't')
                    {
                        return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.Greater, null);
                    }
                    else if (context.Current == 'e')
                    {
                        return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.GreaterEqual, null);
                    }
                    else
                    {
                        throw new FormatException(context.Error());
                    }
                case 'l':
                    if (!context.Next())
                    {
                        return new ParsedModRewriteExpression(invert, ConditionType.PropertyTest, OperationType.SymbolicLink, null);
                    }
                    if (context.Current == 't')
                    {
                        return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.Less, null);
                    }
                    else if (context.Current == 'e')
                    {
                        return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.LessEqual, null);
                    }
                    else
                    {
                        throw new FormatException(context.Error());
                    }
                case 'n':
                    if (!context.Next() || context.Current != 'e')
                    {
                        throw new FormatException(context.Error());
                    }
                    return new ParsedModRewriteExpression(invert, ConditionType.IntComp, OperationType.NotEqual, null);
                default:
                    throw new FormatException(context.Error());
            }
        }

        private static bool IsValidActionCondition(ParsedModRewriteExpression results)
        {
            if (results.Type == ConditionType.IntComp)
            {
                // If the type is an integer, verify operand is actually an int
                int res;
                if (!int.TryParse(results.Operand, out res))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
