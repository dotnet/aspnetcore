// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// Tokenizes a mod_rewrite rule, delimited by spaces.
    /// </summary>
    public static class Tokenizer
    {
        private const char Space = ' ';
        private const char Escape = '\\';
        private const char Tab = '\t';

        /// <summary>
        /// Splits a string on whitespace, ignoring spaces, creating into a list of strings.
        /// </summary>
        /// <param name="rule">The rule to tokenize.</param>
        /// <returns>A list of tokens.</returns>
        public static List<string> Tokenize(string rule)
        {
            // TODO make list of strings a reference to the original rule? (run into problems with escaped spaces).
            // TODO handle "s and probably replace \ character with no slash.
            if (string.IsNullOrEmpty(rule))
            {
                return null;
            }
            var context = new ParserContext(rule);
            if (!context.Next())
            {
                return null;
            }

            var tokens = new List<string>();
            context.Mark();
            while (true)
            {
                if (!context.Next())
                {
                    // End of string. Capture.
                    break;
                }
                else if (context.Current == Escape)
                {
                    // Need to progress such that the next character is not evaluated.
                    if (!context.Next())
                    {
                        // Means that a character was not escaped appropriately Ex: "foo\"
                        throw new ArgumentException();
                    }
                }
                else if (context.Current == Space || context.Current == Tab)
                {
                    // time to capture!
                    var token = context.Capture();
                    if (!string.IsNullOrEmpty(token))
                    {
                        tokens.Add(token);
                        while (context.Current == Space || context.Current == Tab)
                        {
                            if (!context.Next())
                            {
                                // At end of string, we can return at this point.
                                return tokens;
                            }
                        }
                        context.Mark();
                    }
                }
            }
            var done = context.Capture();
            if (!string.IsNullOrEmpty(done))
            {
                tokens.Add(done);
            }
            return tokens;
        }
    }
}
