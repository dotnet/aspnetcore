// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Rewrite.Internal.IISUrlRewrite
{
    public class UrlRewriteFileParser
    {
        private readonly InputParser _inputParser = new InputParser();

        public IList<IISUrlRewriteRule> Parse(TextReader reader)
        {
            var xmlDoc = XDocument.Load(reader, LoadOptions.SetLineInfo);
            var xmlRoot = xmlDoc.Descendants(RewriteTags.Rewrite).FirstOrDefault();

            if (xmlRoot != null)
            {
                var result = new List<IISUrlRewriteRule>();
                // TODO Global rules are currently not treated differently than normal rules, fix.
                // See: https://github.com/aspnet/BasicMiddleware/issues/59
                ParseRules(xmlRoot.Descendants(RewriteTags.GlobalRules).FirstOrDefault(), result);
                ParseRules(xmlRoot.Descendants(RewriteTags.Rules).FirstOrDefault(), result);
                return result;
            }
            return null;
        }

        private void ParseRules(XElement rules, IList<IISUrlRewriteRule> result)
        {
            if (rules == null)
            {
                return;
            }

            if (string.Equals(rules.Name.ToString(), "GlobalRules", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Support for global rules has not been implemented yet");
            }

            foreach (var rule in rules.Elements(RewriteTags.Rule))
            {
                var builder = new UrlRewriteRuleBuilder();
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
                ThrowUrlFormatException(rule, "Cannot have rule without match");
            }

            var action = rule.Element(RewriteTags.Action);
            if (action == null)
            {
                ThrowUrlFormatException(rule, "Rule does not have an associated action attribute");
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
                ThrowUrlFormatException(match, "Match must have Url Attribute");
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
            builder.AddUrlConditions(grouping, trackAllCaptures);

            foreach (var cond in conditions.Elements(RewriteTags.Add))
            {
                ParseCondition(cond, builder, patternSyntax);
            }
        }

        private void ParseCondition(XElement condition, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            var ignoreCase = ParseBool(condition, RewriteTags.IgnoreCase, defaultValue: true);
            var negate = ParseBool(condition, RewriteTags.Negate, defaultValue: false);
            var matchType = ParseEnum(condition, RewriteTags.MatchType, MatchType.Pattern);
            var parsedInputString = condition.Attribute(RewriteTags.Input)?.Value;

            if (parsedInputString == null)
            {
                ThrowUrlFormatException(condition, "Conditions must have an input attribute");
            }

            var parsedPatternString = condition.Attribute(RewriteTags.Pattern)?.Value;
            try
            {
                var input = _inputParser.ParseInputString(parsedInputString);
                builder.AddUrlCondition(input, parsedPatternString, patternSyntax, matchType, ignoreCase, negate);
            }
            catch (FormatException formatException)
            {
                ThrowUrlFormatException(condition, formatException.Message, formatException);
            }
        }

        private void ParseUrlAction(XElement urlAction, UrlRewriteRuleBuilder builder, bool stopProcessing)
        {
            var actionType = ParseEnum(urlAction, RewriteTags.Type, ActionType.None);
            var redirectType = ParseEnum(urlAction, RewriteTags.RedirectType, RedirectType.Permanent);
            var appendQuery = ParseBool(urlAction, RewriteTags.AppendQueryString, defaultValue: true);

            string url = string.Empty;
            if (urlAction.Attribute(RewriteTags.Url) != null)
            {
                url = urlAction.Attribute(RewriteTags.Url).Value;
                if (string.IsNullOrEmpty(url))
                {
                    ThrowUrlFormatException(urlAction, "Url attribute cannot contain an empty string");
                }
            }

            try
            {
                var input = _inputParser.ParseInputString(url);
                builder.AddUrlAction(input, actionType, appendQuery, stopProcessing, (int)redirectType);
            }
            catch (FormatException formatException)
            {
                ThrowUrlFormatException(urlAction, formatException.Message, formatException);
            }
        }

        private static void ThrowUrlFormatException(XElement element, string message)
        {
            var lineInfo = (IXmlLineInfo)element;
            var line = lineInfo.LineNumber;
            var col = lineInfo.LinePosition;
            throw new FormatException(Resources.FormatError_UrlRewriteParseError(message, line, col));
        }

        private static void ThrowUrlFormatException(XElement element, string message, Exception ex)
        {
            var lineInfo = (IXmlLineInfo)element;
            var line = lineInfo.LineNumber;
            var col = lineInfo.LinePosition;
            throw new FormatException(Resources.FormatError_UrlRewriteParseError(message, line, col), ex);
        }

        private static void ThrowParameterFormatException(XElement element, string message)
        {
            var lineInfo = (IXmlLineInfo)element;
            var line = lineInfo.LineNumber;
            var col = lineInfo.LinePosition;
            throw new FormatException(Resources.FormatError_UrlRewriteParseError(message, line, col));
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
                ThrowParameterFormatException(element, $"The {rewriteTag} parameter '{attribute.Value}' was not recognized");
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
                ThrowParameterFormatException(element, $"The {rewriteTag} parameter '{attribute.Value}' was not recognized");
            }
            return enumResult;
        }
    }
}
