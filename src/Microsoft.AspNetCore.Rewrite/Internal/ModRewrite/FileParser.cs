// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class FileParser
    {
        public List<Rule> Parse(TextReader input)
        {
            string line = null;
            var rules = new List<Rule>();
            var builder = new RuleBuilder();
            var lineNum = 0;
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
                var tokens = Tokenizer.Tokenize(line);
                if (tokens.Count > 4)
                {
                    // This means the line didn't have an appropriate format, throw format exception
                    throw new FormatException(Resources.FormatError_ModRewriteParseError("Too many tokens on line", lineNum));
                }

                switch (tokens[0])
                {
                    case "RewriteBase":
                        throw new NotImplementedException("RewriteBase is not implemented");
                    case "RewriteCond":
                        try
                        {
                            var pattern = TestStringParser.Parse(tokens[1]);
                            var condActionParsed = ConditionPatternParser.ParseActionCondition(tokens[2]);

                            var flags = new Flags();
                            if (tokens.Count == 4)
                            {
                                flags = FlagParser.Parse(tokens[3]);
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
                            var regex = RuleRegexParser.ParseRuleRegex(tokens[1]);
                            var pattern = TestStringParser.Parse(tokens[2]);

                            // TODO see if we can have flags be null.
                            var flags = new Flags();
                            if (tokens.Count == 4)
                            {
                                flags = FlagParser.Parse(tokens[3]);
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
                        throw new NotImplementedException("RewriteMap to be added soon.");
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
