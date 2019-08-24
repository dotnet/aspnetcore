// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite
{
    internal class UrlRewriteFileParser
    {
        private InputParser _inputParser;

        /// <summary>
        /// Parse an IIS rewrite section into a list of <see cref="IISUrlRewriteRule"/>s.
        /// </summary>
        /// <param name="reader">The reader containing the rewrite XML</param>
        /// <param name="alwaysUseManagedServerVariables">Determines whether server variables will be sourced from the managed server</param>
        public IList<IISUrlRewriteRule> Parse(TextReader reader, bool alwaysUseManagedServerVariables)
        {
            var xmlDoc = XDocument.Load(reader, LoadOptions.SetLineInfo);
            var xmlRoot = xmlDoc.Descendants(RewriteTags.Rewrite).FirstOrDefault();

            if (xmlRoot == null)
            {
                throw new InvalidUrlRewriteFormatException(new XElement(RewriteTags.Rewrite), "The root element '<rewrite>' is missing");
            }

            _inputParser = new InputParser(RewriteMapParser.Parse(xmlRoot), alwaysUseManagedServerVariables);

            var result = new List<IISUrlRewriteRule>();
            ParseRules(xmlRoot.Descendants(RewriteTags.GlobalRules).FirstOrDefault(), result, global: true);
            ParseRules(xmlRoot.Descendants(RewriteTags.Rules).FirstOrDefault(), result, global: false);
            return result;
        }

        private void ParseRules(XElement rules, IList<IISUrlRewriteRule> result, bool global)
        {
            if (rules == null)
            {
                return;
            }

            foreach (var rule in rules.Elements(RewriteTags.Rule))
            {
                var builder = new UrlRewriteRuleBuilder { Global = global };
                ParseRuleAttributes(rule, builder);

                if (builder.Enabled)
                {
                    result.Add(builder.Build());
                }
            }
        }

        private void ParseRuleAttributes(XElement rule, UrlRewriteRuleBuilder builder)
        {
            builder.Name = rule.Attribute(RewriteTags.Name)?.Value;

            if (ParseBool(rule, RewriteTags.Enabled, defaultValue: true))
            {
                builder.Enabled = true;
            }
            else
            {
                return;
            }

            var patternSyntax = ParseEnum(rule, RewriteTags.PatternSyntax, PatternSyntax.ECMAScript);
            var stopProcessing = ParseBool(rule, RewriteTags.StopProcessing, defaultValue: false);

            var match = rule.Element(RewriteTags.Match);
            if (match == null)
            {
                throw new InvalidUrlRewriteFormatException(rule, "Condition must have an associated match");
            }

            var action = rule.Element(RewriteTags.Action);
            if (action == null)
            {
                throw new InvalidUrlRewriteFormatException(rule, "Rule does not have an associated action attribute");
            }

            ParseMatch(match, builder, patternSyntax);
            ParseConditions(rule.Element(RewriteTags.Conditions), builder, patternSyntax);
            ParseUrlAction(action, builder, stopProcessing);
        }

        private void ParseMatch(XElement match, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            var parsedInputString = match.Attribute(RewriteTags.Url)?.Value;
            if (parsedInputString == null)
            {
                throw new InvalidUrlRewriteFormatException(match, "Match must have Url Attribute");
            }

            var ignoreCase = ParseBool(match, RewriteTags.IgnoreCase, defaultValue: true);
            var negate = ParseBool(match, RewriteTags.Negate, defaultValue: false);
            builder.AddUrlMatch(parsedInputString, ignoreCase, negate, patternSyntax);
        }

        private void ParseConditions(XElement conditions, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            if (conditions == null)
            {
                return;
            }

            var grouping = ParseEnum(conditions, RewriteTags.LogicalGrouping, LogicalGrouping.MatchAll);
            var trackAllCaptures = ParseBool(conditions, RewriteTags.TrackAllCaptures, defaultValue: false);
            var adds = conditions.Elements(RewriteTags.Add);
            if (!adds.Any())
            {
                return;
            }

            builder.ConfigureConditionBehavior(grouping, trackAllCaptures);

            foreach (var cond in adds)
            {
                ParseCondition(cond, builder, patternSyntax);
            }
        }

        private void ParseCondition(XElement conditionElement, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            var ignoreCase = ParseBool(conditionElement, RewriteTags.IgnoreCase, defaultValue: true);
            var negate = ParseBool(conditionElement, RewriteTags.Negate, defaultValue: false);
            var matchType = ParseEnum(conditionElement, RewriteTags.MatchType, MatchType.Pattern);
            var parsedInputString = conditionElement.Attribute(RewriteTags.Input)?.Value;

            if (parsedInputString == null)
            {
                throw new InvalidUrlRewriteFormatException(conditionElement, "Conditions must have an input attribute");
            }

            var parsedPatternString = conditionElement.Attribute(RewriteTags.Pattern)?.Value;
            Condition condition;

            switch (patternSyntax)
            {
                case PatternSyntax.ECMAScript:
                {
                    switch (matchType)
                    {
                        case MatchType.Pattern:
                        {
                            if (string.IsNullOrEmpty(parsedPatternString))
                            {
                                throw new FormatException("Match does not have an associated pattern attribute in condition");
                            }
                            condition = new UriMatchCondition(_inputParser, parsedInputString, parsedPatternString, builder.UriMatchPart, ignoreCase, negate);
                            break;
                        }
                        case MatchType.IsDirectory:
                        {
                            condition = new Condition { Input = _inputParser.ParseInputString(parsedInputString, builder.UriMatchPart), Match = new IsDirectoryMatch(negate) };
                            break;
                        }
                        case MatchType.IsFile:
                        {
                            condition = new Condition { Input = _inputParser.ParseInputString(parsedInputString, builder.UriMatchPart), Match = new IsFileMatch(negate) };
                            break;
                        }
                        default:
                            throw new FormatException("Unrecognized matchType");
                    }
                    break;
                }
                case PatternSyntax.Wildcard:
                    throw new NotSupportedException("Wildcard syntax is not supported");
                case PatternSyntax.ExactMatch:
                    if (string.IsNullOrEmpty(parsedPatternString))
                    {
                        throw new FormatException("Match does not have an associated pattern attribute in condition");
                    }
                    condition = new Condition { Input = _inputParser.ParseInputString(parsedInputString, builder.UriMatchPart), Match = new ExactMatch(ignoreCase, parsedPatternString, negate) };
                    break;
                default:
                    throw new FormatException("Unrecognized pattern syntax");
            }

            builder.AddUrlCondition(condition);
        }

        private void ParseUrlAction(XElement urlAction, UrlRewriteRuleBuilder builder, bool stopProcessing)
        {
            var actionType = ParseEnum(urlAction, RewriteTags.Type, ActionType.None);
            UrlAction action;
            switch (actionType)
            {
                case ActionType.None:
                    action = new NoneAction(stopProcessing ? RuleResult.SkipRemainingRules : RuleResult.ContinueRules);
                    break;
                case ActionType.Rewrite:
                case ActionType.Redirect:
                    var url = string.Empty;
                    if (urlAction.Attribute(RewriteTags.Url) != null)
                    {
                        url = urlAction.Attribute(RewriteTags.Url).Value;
                        if (string.IsNullOrEmpty(url))
                        {
                            throw new InvalidUrlRewriteFormatException(urlAction, "Url attribute cannot contain an empty string");
                        }
                    }

                    var urlPattern = _inputParser.ParseInputString(url, builder.UriMatchPart);
                    var appendQuery = ParseBool(urlAction, RewriteTags.AppendQueryString, defaultValue: true);

                    if (actionType == ActionType.Rewrite)
                    {
                        action = new RewriteAction(stopProcessing ? RuleResult.SkipRemainingRules : RuleResult.ContinueRules, urlPattern, appendQuery);
                    }
                    else
                    {
                        var redirectType = ParseEnum(urlAction, RewriteTags.RedirectType, RedirectType.Permanent);
                        action = new RedirectAction((int)redirectType, urlPattern, appendQuery);
                    }
                    break;
                case ActionType.AbortRequest:
                    action = new AbortAction();
                    break;
                case ActionType.CustomResponse:
                    int statusCode;
                    if (!int.TryParse(urlAction.Attribute(RewriteTags.StatusCode)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out statusCode))
                    {
                        throw new InvalidUrlRewriteFormatException(urlAction, "A valid status code is required");
                    }

                    if (statusCode < 200 || statusCode > 999)
                    {
                        throw new NotSupportedException("Status codes must be between 200 and 999 (inclusive)");
                    }

                    if (!string.IsNullOrEmpty(urlAction.Attribute(RewriteTags.SubStatusCode)?.Value))
                    {
                        throw new NotSupportedException("Substatus codes are not supported");
                    }

                    var statusReason = urlAction.Attribute(RewriteTags.StatusReason)?.Value;
                    var statusDescription = urlAction.Attribute(RewriteTags.StatusDescription)?.Value;

                    action = new CustomResponseAction(statusCode) { StatusReason = statusReason, StatusDescription = statusDescription };
                    break;
                default:
                    throw new NotSupportedException($"The action type {actionType} wasn't recognized");
            }
            builder.AddUrlAction(action);
        }

        private bool ParseBool(XElement element, string rewriteTag, bool defaultValue)
        {
            bool result;
            var attribute = element.Attribute(rewriteTag);
            if (attribute == null)
            {
                return defaultValue;
            }
            else if (!bool.TryParse(attribute.Value, out result))
            {
                throw new InvalidUrlRewriteFormatException(element, $"The {rewriteTag} parameter '{attribute.Value}' was not recognized");
            }
            return result;
        }

        private TEnum ParseEnum<TEnum>(XElement element, string rewriteTag, TEnum defaultValue)
            where TEnum : struct
        {
            TEnum enumResult = default(TEnum);
            var attribute = element.Attribute(rewriteTag);
            if (attribute == null)
            {
                return defaultValue;
            }
            else if(!Enum.TryParse(attribute.Value, ignoreCase: true, result: out enumResult))
            {
                throw new InvalidUrlRewriteFormatException(element, $"The {rewriteTag} parameter '{attribute.Value}' was not recognized");
            }
            return enumResult;
        }
    }
}
