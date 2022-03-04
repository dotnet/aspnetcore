// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class FileParser
    {
        public IList<IRule> Parse(TextReader input)
        {
            string line;
            var rules = new List<IRule>();
            var builder = new RuleBuilder();
            var lineNum = 0;

            // parsers
            var testStringParser = new TestStringParser();
            var conditionParser = new ConditionPatternParser();
            var regexParser = new RuleRegexParser();
            var flagsParser = new FlagParser();
            var tokenizer = new Tokenizer();

            while ((line = input.ReadLine()) != null)
            {
                lineNum++;
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                if (line.StartsWith("#"))
                {
                    continue;
                }
                var tokens = tokenizer.Tokenize(line);
                if (tokens.Count > 4)
                {
                    // This means the line didn't have an appropriate format, throw format exception
                    throw new FormatException(Resources.FormatError_ModRewriteParseError("Too many tokens on line", lineNum));
                }

                switch (tokens[0])
                {
                    case "RewriteBase":
                        // the notion of the path base spans across all rules, not just mod_rewrite
                        // So not implemented for now
                        throw new NotImplementedException("RewriteBase is not implemented");
                    case "RewriteCond":
                        try
                        {
                            var pattern = testStringParser.Parse(tokens[1]);
                            var condActionParsed = conditionParser.ParseActionCondition(tokens[2]);

                            var flags = new Flags();
                            if (tokens.Count == 4)
                            {
                                flags = flagsParser.Parse(tokens[3]);
                            }

                            builder.AddConditionFromParts(pattern, condActionParsed, flags);
                        }
                        catch (FormatException formatException)
                        {
                            throw new FormatException(Resources.FormatError_ModRewriteGeneralParseError(lineNum), formatException);
                        }
                        break;
                    case "RewriteRule":
                        try
                        {
                            var regex = regexParser.ParseRuleRegex(tokens[1]);
                            var pattern = testStringParser.Parse(tokens[2]);

                            Flags flags;
                            if (tokens.Count == 4)
                            {
                                flags = flagsParser.Parse(tokens[3]);
                            }
                            else
                            {
                                flags = new Flags();
                            }

                            builder.AddMatch(regex, flags);
                            builder.AddAction(pattern, flags);
                            rules.Add(builder.Build());
                            builder = new RuleBuilder();
                        }
                        catch (FormatException formatException)
                        {
                            throw new FormatException(Resources.FormatError_ModRewriteGeneralParseError(lineNum), formatException);
                        }
                        break;
                    case "RewriteMap":
                        // Lack of use
                        throw new NotImplementedException("RewriteMap are not implemented");
                    case "RewriteEngine":
                        // Explicitly do nothing here, no notion of turning on regex engine.
                        break;
                    default:
                        throw new FormatException(Resources.FormatError_ModRewriteParseError("Unrecognized keyword: " + tokens[0], lineNum));
                }
            }
            return rules;
        }
    }
}
