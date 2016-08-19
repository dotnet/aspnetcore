// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite
{
    public class UrlRewriteRuleBuilder
    {
        private readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);
       
        public string Name { get; set; }
        public bool Enabled { get; set; }

        private UrlMatch _initialMatch;
        private Conditions _conditions;
        private UrlAction _action;
        private bool _matchAny;

        public UrlRewriteRule Build()
        {
            if (_initialMatch == null || _action == null)
            {
                throw new InvalidOperationException("Cannot create UrlRewriteRule without action and match");
            }
            var rule = new UrlRewriteRule();
            rule.Action = _action;
            rule.Conditions = _conditions;
            rule.InitialMatch = _initialMatch;
            rule.Name = Name;
            return rule;
        }
        
        public void AddUrlAction(Pattern url, ActionType actionType = ActionType.None, bool appendQueryString = true, bool stopProcessing = false, int statusCode = StatusCodes.Status301MovedPermanently)
        {
            switch (actionType)
            {
                case ActionType.None:
                    _action =  new VoidAction();
                    break;
                case ActionType.Rewrite:
                    if (appendQueryString)
                    {
                        _action = new RewriteAction(stopProcessing ? RuleTerminiation.StopRules : RuleTerminiation.Continue, url, clearQuery: false);
                    }
                    else
                    {
                        _action = new RewriteAction(stopProcessing ? RuleTerminiation.StopRules : RuleTerminiation.Continue, url, clearQuery: true);
                    }
                    break;
                case ActionType.Redirect:
                    if (appendQueryString)
                    {
                        _action = new RedirectAction(statusCode, url);
                    }
                    else
                    {
                        _action = new RedirectClearQueryAction(statusCode, url);
                    }
                    break;
                case ActionType.AbortRequest:
                    throw new NotImplementedException("Abort Requests are not supported");
                case ActionType.CustomResponse:
                    // TODO
                    throw new NotImplementedException("Custom Responses are not supported");
            }
        }

        public void AddUrlMatch(string input, bool ignoreCase = true, bool negate = false, PatternSyntax patternSyntax = PatternSyntax.ECMAScript)
        {
            switch (patternSyntax)
            {
                case PatternSyntax.ECMAScript:
                    {
                        if (ignoreCase)
                        {
                            var regex = new Regex(input, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);
                            _initialMatch = new RegexMatch(regex, negate);
                        }
                        else
                        {
                            var regex = new Regex(input, RegexOptions.Compiled, RegexTimeout);
                            _initialMatch =  new RegexMatch(regex, negate);
                        }
                        break;
                    }
                case PatternSyntax.WildCard:
                    throw new NotImplementedException("Wildcard syntax is not supported");
                case PatternSyntax.ExactMatch:
                    _initialMatch =  new ExactMatch(ignoreCase, input, negate);
                    break;
            }
        }

        public void AddUrlCondition(Pattern input, string pattern, PatternSyntax patternSyntax, MatchType matchType, bool ignoreCase, bool negate)
        {
            // If there are no conditions specified, 
            if (_conditions == null)
            {
                AddUrlConditions(LogicalGrouping.MatchAll, trackingAllCaptures: false);
            }

            switch (patternSyntax)
            {
                case PatternSyntax.ECMAScript:
                    {
                        switch (matchType)
                        {
                            case MatchType.Pattern:
                                {
                                    if (pattern == null)
                                    {
                                        throw new FormatException("Match does not have an associated pattern attribute in condition");
                                    }

                                    Regex regex = null;
                                    if (ignoreCase)
                                    {
                                        regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout);
                                    }
                                    else
                                    {
                                        regex = new Regex(pattern, RegexOptions.Compiled, RegexTimeout);
                                    }

                                    _conditions.ConditionList.Add(new Condition { Input = input, Match = new RegexMatch(regex, negate), OrNext = _matchAny});
                                    break;
                                }
                            case MatchType.IsDirectory:
                                {
                                    _conditions.ConditionList.Add(new Condition { Input = input, Match = new IsDirectoryMatch(negate), OrNext = _matchAny });
                                    break;
                                }
                            case MatchType.IsFile:
                                {
                                    _conditions.ConditionList.Add(new Condition { Input = input, Match = new IsFileMatch(negate), OrNext = _matchAny });
                                    break;
                                }
                            default:
                                throw new FormatException("Unrecognized matchType");
                        }
                        break;
                    }
                case PatternSyntax.WildCard:
                    throw new NotImplementedException("Wildcard syntax is not supported");
                case PatternSyntax.ExactMatch:
                    if (pattern == null)
                    {
                        throw new FormatException("Match does not have an associated pattern attribute in condition");
                    }
                    _conditions.ConditionList.Add(new Condition { Input = input, Match = new ExactMatch(ignoreCase, pattern, negate), OrNext = _matchAny });
                    break;
                default:
                    throw new FormatException("Unrecognized pattern syntax");
            }
        }

        public void AddUrlConditions(LogicalGrouping logicalGrouping, bool trackingAllCaptures)
        {
            var conditions = new Conditions();
            conditions.ConditionList = new List<Condition>();
            _matchAny = logicalGrouping == LogicalGrouping.MatchAny;
            _conditions = conditions;
        }
    }
}
