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

            bool enabled;
            if (!bool.TryParse(rule.Attribute(RewriteTags.Enabled)?.Value, out enabled))
            {
                builder.Enabled = true;
            }
            else
            {
                if (enabled)
                {
                    builder.Enabled = enabled;
                }
                else
                {
                    return;
                }
            }

            PatternSyntax patternSyntax;
            if (rule.Attribute(RewriteTags.PatternSyntax) == null)
            {
                patternSyntax = PatternSyntax.ECMAScript;
            }
            else if (!Enum.TryParse(rule.Attribute(RewriteTags.PatternSyntax).Value, ignoreCase: true, result: out patternSyntax))
            {
                ThrowParameterFormatException(rule, $"The {RewriteTags.PatternSyntax} parameter '{rule.Attribute(RewriteTags.PatternSyntax).Value}' was not recognized");
            }

            bool stopProcessing;
            if (!bool.TryParse(rule.Attribute(RewriteTags.StopProcessing)?.Value, out stopProcessing))
            {
                stopProcessing = false;
            }

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

            bool ignoreCase;
            if (!bool.TryParse(match.Attribute(RewriteTags.IgnoreCase)?.Value, out ignoreCase))
            {
                ignoreCase = true;
            }

            bool negate;
            if (!bool.TryParse(match.Attribute(RewriteTags.Negate)?.Value, out negate))
            {
                negate = false;
            }
            builder.AddUrlMatch(parsedInputString, ignoreCase, negate, patternSyntax);
        }

        private void ParseConditions(XElement conditions, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            if (conditions == null)
            {
                return;
            }

            LogicalGrouping grouping;
            if (conditions.Attribute(RewriteTags.LogicalGrouping) == null)
            {
                grouping = LogicalGrouping.MatchAll;
            }
            else if (!Enum.TryParse(conditions.Attribute(RewriteTags.LogicalGrouping).Value, ignoreCase: true, result: out grouping))
            {
                ThrowParameterFormatException(conditions, $"The {RewriteTags.LogicalGrouping} parameter '{conditions.Attribute(RewriteTags.LogicalGrouping).Value}' was not recognized");
            }

            bool trackingAllCaptures;
            if (!bool.TryParse(conditions.Attribute(RewriteTags.TrackingAllCaptures)?.Value, out trackingAllCaptures))
            {
                trackingAllCaptures = false;
            }

            builder.AddUrlConditions(grouping, trackingAllCaptures);

            foreach (var cond in conditions.Elements(RewriteTags.Add))
            {
                ParseCondition(cond, builder, patternSyntax);
            }
        }

        private void ParseCondition(XElement condition, UrlRewriteRuleBuilder builder, PatternSyntax patternSyntax)
        {
            bool ignoreCase;
            if (!bool.TryParse(condition.Attribute(RewriteTags.IgnoreCase)?.Value, out ignoreCase))
            {
                ignoreCase = true;
            }

            bool negate;
            if (!bool.TryParse(condition.Attribute(RewriteTags.Negate)?.Value, out negate))
            {
                negate = false;
            }

            MatchType matchType;
            if (condition.Attribute(RewriteTags.MatchType) == null)
            {
                matchType = MatchType.Pattern;
            }
            else if (!Enum.TryParse(condition.Attribute(RewriteTags.MatchType).Value, ignoreCase: true, result: out matchType))
            {
                ThrowParameterFormatException(condition, $"The {RewriteTags.MatchType} parameter '{condition.Attribute(RewriteTags.MatchType).Value}' was not recognized");
            }

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
            ActionType actionType;
            if (urlAction.Attribute(RewriteTags.Type) == null)
            {
                actionType = ActionType.None;
            }
            else if (!Enum.TryParse(urlAction.Attribute(RewriteTags.Type).Value, ignoreCase: true, result: out actionType))
            {
                ThrowParameterFormatException(urlAction, $"The {RewriteTags.Type} parameter '{urlAction.Attribute(RewriteTags.Type).Value}' was not recognized");
            }

            bool appendQuery;
            if (!bool.TryParse(urlAction.Attribute(RewriteTags.AppendQueryString)?.Value, out appendQuery))
            {
                appendQuery = true;
            }

            RedirectType redirectType;
            if (urlAction.Attribute(RewriteTags.RedirectType) == null)
            {
                redirectType = RedirectType.Permanent;
            }
            else if (!Enum.TryParse(urlAction.Attribute(RewriteTags.RedirectType).Value, ignoreCase: true, result: out redirectType))
            {
                ThrowParameterFormatException(urlAction, $"The {RewriteTags.RedirectType} parameter '{urlAction.Attribute(RewriteTags.RedirectType).Value}' was not recognized");
            }

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
    }
}
