// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Rewrite.Internal;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileParser
    {
        public static List<Rule> Parse(TextReader input)
        {
            string line = null;
            var rules = new List<Rule>();
            var conditions = new List<Condition>();
            // TODO consider passing Itokenizer and Ifileparser and provide implementations
            while ((line = input.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                if (line.StartsWith("#"))
                {
                    continue;
                }
                var tokens = Tokenizer.Tokenize(line);
                if (tokens.Count > 4)
                {
                    // This means the line didn't have an appropriate format, throw format exception
                    throw new FormatException();
                }
                // TODO make a new class called rule parser that does and either return an exception or return the rule.
                switch (tokens[0])
                {
                    case "RewriteBase":
                        throw new NotSupportedException();
                    //if (tokens.Count == 2)
                    //{
                    //    ModRewriteBase.Base = tokens[1];
                    //}
                    //else
                    //{
                    //    throw new FormatException("");
                    //}
                    //break;
                    case "RewriteCond":
                        {
                            ConditionBuilder builder = null;
                            if (tokens.Count == 3)
                            {
                                builder = new ConditionBuilder(tokens[1], tokens[2]);
                            }
                            else if (tokens.Count == 4)
                            {
                                builder = new ConditionBuilder(tokens[1], tokens[2], tokens[3]);
                            }
                            else
                            {
                                throw new FormatException();
                            }
                            conditions.Add(builder.Build());
                            break;
                        }
                    case "RewriteRule":
                        {
                            RuleBuilder builder = null;
                            if (tokens.Count == 3)
                            {
                                builder = new RuleBuilder(tokens[1], tokens[2]);
                            }
                            else if (tokens.Count == 4)
                            {
                                builder = new RuleBuilder(tokens[1], tokens[2], tokens[3]);
                            }
                            else
                            {
                                throw new FormatException();
                            }
                            builder.AddConditions(conditions);
                            rules.Add(builder.Build());
                            conditions = new List<Condition>();
                            break;
                        }
                    case "RewriteMap":
                        throw new NotImplementedException("RewriteMaps to be added soon.");
                    case "RewriteEngine":
                        // Explicitly do nothing here, no notion of turning on regex engine.
                        break;
                    default:
                        throw new FormatException(tokens[0]);
                }
            }
            return rules;
        }

    }
}
