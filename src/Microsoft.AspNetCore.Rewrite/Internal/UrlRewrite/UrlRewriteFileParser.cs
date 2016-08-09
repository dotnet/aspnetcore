// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public static class UrlRewriteFileParser
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);

        public static List<UrlRewriteRule> Parse(TextReader reader)
        {
            var xmlDoc = XDocument.Load(reader, LoadOptions.SetLineInfo);
            var xmlRoot = xmlDoc.Descendants(RewriteTags.Rewrite).FirstOrDefault();

            if (xmlRoot != null)
            {
                var result = new List<UrlRewriteRule>();
                // TODO Global rules are currently not treated differently than normal rules, fix. 
                // See: https://github.com/aspnet/BasicMiddleware/issues/59
                ParseRules(xmlRoot.Descendants(RewriteTags.GlobalRules).FirstOrDefault(), result, isGlobalRule: true);
                ParseRules(xmlRoot.Descendants(RewriteTags.Rules).FirstOrDefault(), result, isGlobalRule: false);
                return result;
            }
            return null;
        }

        private static void ParseRules(XElement rules, List<UrlRewriteRule> result, bool isGlobalRule)
        {
            if (rules == null)
            {
                return;
            }

            foreach (var rule in rules.Elements(RewriteTags.Rule))
            {
                var res = new UrlRewriteRule();
                SetRuleAttributes(rule, res);
                var action = rule.Element(RewriteTags.Action);
                if (action == null)
                {
                    ThrowUrlFormatException(rule, "Rule does not have an associated action attribute");
                }
                CreateUrlAction(action, res, isGlobalRule);
                if (res.Enabled)
                {
                    result.Add(res);
                }
            }
        }

        private static void SetRuleAttributes(XElement rule, UrlRewriteRule res)
        {

            res.Name = rule.Attribute(RewriteTags.Name)?.Value;

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
            var match = rule.Element(RewriteTags.Match);
            if (match == null)
            {
                ThrowUrlFormatException(rule, "Cannot have rule without match");
            }
            CreateMatch(match, res);
            CreateConditions(rule.Element(RewriteTags.Conditions), res);
        }

        private static void CreateMatch(XElement match, UrlRewriteRule res)
        {
            var matchRes = new ParsedUrlMatch();

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
            if (parsedInputString == null)
            {
                ThrowUrlFormatException(match, "Match must have Url Attribute");
            }

            switch (res.PatternSyntax)
            {
                case PatternSyntax.ECMAScript:
                    {
                        if (matchRes.IgnoreCase)
                        {
                            var regex = new Regex(parsedInputString, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);
                            res.InitialMatch = new RegexMatch(regex, matchRes.Negate);
                        }
                        else
                        {
                            var regex = new Regex(parsedInputString, RegexOptions.Compiled, RegexTimeout);
                            res.InitialMatch = new RegexMatch(regex, matchRes.Negate);
                        }
                    }
                    break;
                case PatternSyntax.WildCard:
                    throw new NotImplementedException("Wildcard syntax is not supported.");
                case PatternSyntax.ExactMatch:
                    res.InitialMatch = new ExactMatch(matchRes.IgnoreCase, parsedInputString, matchRes.Negate);
                    break;
            }
        }

        private static void CreateConditions(XElement conditions, UrlRewriteRule res)
        {
            // This is to avoid nullptr exception on referencing conditions.
            res.Conditions = new Conditions();
            if (conditions == null)
            {
                return;
            }

            LogicalGrouping grouping;
            if (Enum.TryParse(conditions.Attribute(RewriteTags.MatchType)?.Value, out grouping))
            {
                res.Conditions.MatchType = grouping;
            }

            bool parBool;
            if (bool.TryParse(conditions.Attribute(RewriteTags.TrackingAllCaptures)?.Value, out parBool))
            {
                res.Conditions.TrackingAllCaptures = parBool;
            }

            foreach (var cond in conditions.Elements(RewriteTags.Add))
            {
                CreateCondition(cond, res);
            }
        }

        private static void CreateCondition(XElement condition, UrlRewriteRule res)
        {
            var parsedCondRes = new ParsedCondition();

            bool parBool;
            if (bool.TryParse(condition.Attribute(RewriteTags.IgnoreCase)?.Value, out parBool))
            {
                parsedCondRes.IgnoreCase = parBool;
            }

            if (bool.TryParse(condition.Attribute(RewriteTags.Negate)?.Value, out parBool))
            {
                parsedCondRes.Negate = parBool;
            }

            MatchType matchType;
            if (Enum.TryParse(condition.Attribute(RewriteTags.MatchType)?.Value, out matchType))
            {
                parsedCondRes.MatchType = matchType;
            }

            var parsedString = condition.Attribute(RewriteTags.Input)?.Value;
            if (parsedString == null)
            {
                ThrowUrlFormatException(condition, "Conditions must have an input attribute");
            }

            Pattern input = null;
            try
            {
                input = InputParser.ParseInputString(parsedString);
            }
            catch (FormatException formatException)
            {
                ThrowUrlFormatException(condition, formatException.Message, formatException);
            }

            switch (res.PatternSyntax)
            {
                case PatternSyntax.ECMAScript:
                    {
                        switch (parsedCondRes.MatchType)
                        {
                            case MatchType.Pattern:
                                {
                                    parsedString = condition.Attribute(RewriteTags.Pattern)?.Value;
                                    if (parsedString == null)
                                    {
                                        ThrowUrlFormatException(condition, "Match does not have an associated pattern attribute in condition");

                                    }
                                    Regex regex = null;

                                    if (parsedCondRes.IgnoreCase)
                                    {
                                        regex = new Regex(parsedString, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);
                                    }
                                    else
                                    {
                                        regex = new Regex(parsedString, RegexOptions.Compiled, RegexTimeout);
                                    }

                                    res.Conditions.ConditionList.Add(new Condition { Input = input, Match = new RegexMatch(regex, parsedCondRes.Negate) });
                                }
                                break;
                            case MatchType.IsDirectory:
                                {
                                    res.Conditions.ConditionList.Add(new Condition { Input = input, Match = new IsDirectoryMatch(parsedCondRes.Negate) });
                                }
                                break;
                            case MatchType.IsFile:
                                {
                                    res.Conditions.ConditionList.Add(new Condition { Input = input, Match = new IsFileMatch(parsedCondRes.Negate) });
                                }
                                break;
                            default:
                                // TODO don't this this can ever be thrown
                                ThrowUrlFormatException(condition, "Unrecognized matchType");
                                break;
                        }
                    }
                    break;
                case PatternSyntax.WildCard:
                    throw new NotImplementedException("Wildcard syntax is not supported");
                case PatternSyntax.ExactMatch:
                    parsedString = condition.Attribute(RewriteTags.Pattern)?.Value;
                    if (parsedString == null)
                    {
                        ThrowUrlFormatException(condition, "Pattern match does not have an associated pattern attribute in condition");
                    }
                    res.Conditions.ConditionList.Add(new Condition { Input = input, Match = new ExactMatch(parsedCondRes.IgnoreCase, parsedString, parsedCondRes.Negate) });
                    break;
                default:
                    ThrowUrlFormatException(condition, "Unrecognized pattern syntax");
                    break;
            }
        }

        private static void CreateUrlAction(XElement urlAction, UrlRewriteRule res, bool globalRule)
        {

            var actionRes = new ParsedUrlAction();

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

            try
            {
                actionRes.Url = InputParser.ParseInputString(urlAction.Attribute(RewriteTags.Url)?.Value);
            }
            catch (FormatException formatException)
            {
                ThrowUrlFormatException(urlAction, formatException.Message, formatException);
            }

            CreateUrlActionFromParsedAction(urlAction, actionRes, globalRule, res);
        }

        private static void CreateUrlActionFromParsedAction(XElement urlAction, ParsedUrlAction actionRes, bool globalRule, UrlRewriteRule res)
        {
            switch (actionRes.Type)
            {
                case ActionType.None:
                    res.Action = new VoidAction();
                    break;
                case ActionType.Rewrite:
                    if (actionRes.AppendQueryString)
                    {
                        res.Action = new RewriteAction(res.StopProcessing ? RuleTerminiation.StopRules : RuleTerminiation.Continue, actionRes.Url, clearQuery: false);
                    }
                    else
                    {
                        res.Action = new RewriteAction(res.StopProcessing ? RuleTerminiation.StopRules : RuleTerminiation.Continue, actionRes.Url, clearQuery: true);
                    }
                    break;
                case ActionType.Redirect:
                    if (actionRes.AppendQueryString)
                    {
                        res.Action = new RedirectAction((int)actionRes.RedirectType, actionRes.Url);
                    }
                    else
                    {
                        res.Action = new RedirectClearQueryAction((int)actionRes.RedirectType, actionRes.Url);
                    }
                    break;
                case ActionType.AbortRequest:
                    ThrowUrlFormatException(urlAction, "Abort Requests are not supported.");
                    break;
                case ActionType.CustomResponse:
                    // TODO
                    ThrowUrlFormatException(urlAction, "Custom Responses are not supported");
                    break;
            }
        }

        private static void ThrowUrlFormatException(XElement element, string message)
        {
            var line = ((IXmlLineInfo)element).LineNumber;
            var col = ((IXmlLineInfo)element).LinePosition;
            throw new FormatException(Resources.FormatError_UrlRewriteParseError(message, line, col));
        }
        private static void ThrowUrlFormatException(XElement element, string message, Exception ex)
        {
            var line = ((IXmlLineInfo)element).LineNumber;
            var col = ((IXmlLineInfo)element).LinePosition;
            throw new FormatException(Resources.FormatError_UrlRewriteParseError(message, line, col), ex);
        }
    }
}
