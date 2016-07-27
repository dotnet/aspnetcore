// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite
{
    // TODO rename 
    public static class UrlRewriteFileParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);
        public static List<UrlRewriteRule> Parse(TextReader reader)
        {
            var temp = XDocument.Load(reader);
            var xmlRoot = temp.Descendants(RewriteTags.Rewrite);
            var rules = new List<UrlRewriteRule>();

            if (xmlRoot != null)
            {
                // there is a valid rewrite block, go through each rule and process
                GetGlobalRules(xmlRoot.Descendants(RewriteTags.GlobalRules), rules);
                GetRules(xmlRoot.Descendants(RewriteTags.Rules), rules);
            }
            return rules;
        }

        private static void GetGlobalRules(IEnumerable<XElement> globalRules, List<UrlRewriteRule> result)
        {
            foreach (var rule in globalRules.Elements(RewriteTags.Rule) ?? Enumerable.Empty<XElement>())
            {
                var res = new UrlRewriteRule();
                SetRuleAttributes(rule, res);
                // TODO handle full url with global rules - may or may not support
                res.Action = CreateUrlAction(rule.Element(RewriteTags.Action));
                result.Add(res);
            }
        }

        private static void GetRules(IEnumerable<XElement> rules, List<UrlRewriteRule> result)
        {
            // TODO Better null check?
            foreach (var rule in rules.Elements(RewriteTags.Rule) ?? Enumerable.Empty<XElement>())
            {
                var res = new UrlRewriteRule();
                SetRuleAttributes(rule, res);
                res.Action = CreateUrlAction(rule.Element(RewriteTags.Action));
                result.Add(res);
            }
        }

        private static void SetRuleAttributes(XElement rule, UrlRewriteRule res)
        {
            if (rule == null)
            {
                return;
            }

            res.Name =  rule.Attribute(RewriteTags.Name)?.Value;

            bool enabled;
            if (bool.TryParse(rule.Attribute(RewriteTags.Enabled)?.Value, out enabled))
            {
                res.Enabled = enabled;
            }

            PatternSyntax patternSyntax;
            if (Enum.TryParse(rule.Attribute(RewriteTags.PatternSyntax)?.Value, out patternSyntax))
            {
                res.PatternSyntax = patternSyntax;
            }

            bool stopProcessing;
            if (bool.TryParse(rule.Attribute(RewriteTags.StopProcessing)?.Value, out stopProcessing))
            {
                res.StopProcessing = stopProcessing;
            }

            res.Match = CreateMatch(rule.Element(RewriteTags.Match));
            res.Conditions = CreateConditions(rule.Element(RewriteTags.Conditions));
        }

        private static InitialMatch CreateMatch(XElement match)
        {
            if (match == null)
            {
                return null;
            }

            var matchRes = new InitialMatch();

            bool parBool;
            if (bool.TryParse(match.Attribute(RewriteTags.IgnoreCase)?.Value, out parBool))
            {
                matchRes.IgnoreCase = parBool;
            }

            if (bool.TryParse(match.Attribute(RewriteTags.Negate)?.Value, out parBool))
            {
                matchRes.Negate = parBool;
            }

            var parsedInputString = match.Attribute(RewriteTags.Url)?.Value;

            if (matchRes.IgnoreCase)
            {
                matchRes.Url = new Regex(parsedInputString, RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
            }
            else
            {
                matchRes.Url = new Regex(parsedInputString, RegexOptions.Compiled, RegexTimeout);
            }
            return matchRes;
        }


        private static Conditions CreateConditions(XElement conditions)
        {
            var condRes = new Conditions();
            if (conditions == null)
            {
                return condRes; // TODO make sure no null exception on Conditions
            }

            LogicalGrouping grouping;
            if (Enum.TryParse(conditions.Attribute(RewriteTags.MatchType)?.Value, out grouping))
            {
                condRes.MatchType = grouping;
            }

            bool parBool;
            if (bool.TryParse(conditions.Attribute(RewriteTags.TrackingAllCaptures)?.Value, out parBool))
            {
                condRes.TrackingAllCaptures = parBool;
            }

            foreach (var cond in conditions.Elements(RewriteTags.Add))
            {
                condRes.ConditionList.Add(CreateCondition(cond));
            }
            return condRes;
        }

        private static Condition CreateCondition(XElement condition)
        {
            if (condition == null)
            {
                return null;
            }

            var condRes = new Condition();

            bool parBool;
            if (bool.TryParse(condition.Attribute(RewriteTags.IgnoreCase)?.Value, out parBool))
            {
                condRes.IgnoreCase = parBool;
            }

            if (bool.TryParse(condition.Attribute(RewriteTags.Negate)?.Value, out parBool))
            {
                condRes.Negate = parBool;
            }

            MatchType matchType;
            if (Enum.TryParse(condition.Attribute(RewriteTags.MatchPattern)?.Value, out matchType))
            {
                condRes.MatchType = matchType;
            }

            var parsedInputString = condition.Attribute(RewriteTags.Input)?.Value;
            if (parsedInputString != null)
            {
                condRes.Input = InputParser.ParseInputString(parsedInputString);
            }

            parsedInputString = condition.Attribute(RewriteTags.Pattern)?.Value;

            if (condRes.IgnoreCase)
            {
                condRes.MatchPattern = new Regex(parsedInputString, RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout);
            }
            else
            {
                condRes.MatchPattern = new Regex(parsedInputString, RegexOptions.Compiled, RegexTimeout);
            }
            return condRes;
        }

        private static UrlAction CreateUrlAction(XElement urlAction)
        {
            if (urlAction == null)
            {
                throw new FormatException("Action is a required element of a rule.");
            }
            var actionRes = new UrlAction();

            ActionType actionType;
            if (Enum.TryParse(urlAction.Attribute(RewriteTags.Type)?.Value, out actionType))
            {
                actionRes.Type = actionType;
            }

            bool parseBool;
            if (bool.TryParse(urlAction.Attribute(RewriteTags.AppendQuery)?.Value, out parseBool))
            {
                actionRes.AppendQueryString = parseBool;
            }

            if (bool.TryParse(urlAction.Attribute(RewriteTags.LogRewrittenUrl)?.Value, out parseBool))
            {
                actionRes.LogRewrittenUrl = parseBool;
            }

            RedirectType redirectType;
            if (Enum.TryParse(urlAction.Attribute(RewriteTags.RedirectType)?.Value, out redirectType))
            {
                actionRes.RedirectType = redirectType;
            }

            actionRes.Url = InputParser.ParseInputString(urlAction.Attribute(RewriteTags.Url)?.Value);
            return actionRes;
        }
    }
}
