// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal sealed class FileParser
{
    public static IList<IRule> Parse(TextReader input)
    {
        string? line;
        var rules = new List<IRule>();
        var builder = new RuleBuilder();
        var lineNum = 0;

        while ((line = input.ReadLine()) != null)
        {
            lineNum++;
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            if (line.StartsWith('#'))
            {
                continue;
            }
            var tokens = Tokenizer.Tokenize(line)!;
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

                        Flags flags;
                        if (tokens.Count == 4)
                        {
                            flags = FlagParser.Parse(tokens[3]);
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
