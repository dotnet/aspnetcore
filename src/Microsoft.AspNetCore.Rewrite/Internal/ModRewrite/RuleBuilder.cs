// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal.PreActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlActions;
using Microsoft.AspNetCore.Rewrite.Internal.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class RuleBuilder
    {
        private Conditions _conditions;
        private UrlAction _action;
        private UrlMatch _match;
        private List<PreAction> _preActions;

        private readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);

        public ModRewriteRule Build()
        {
            if (_action == null || _match == null)
            {
                // TODO throw an exception here, find apporpriate exception
            }
            return new ModRewriteRule(_match, _conditions, _action, _preActions);
        }

        public void AddRule(string rule)
        {
            // TODO 
            var tokens = new Tokenizer().Tokenize(rule);
            var regex = new RuleRegexParser().ParseRuleRegex(tokens[1]);
            var pattern = new TestStringParser().Parse(tokens[2]);

            Flags flags;
            if (tokens.Count == 4)
            {
                flags = new FlagParser().Parse(tokens[3]);
            }
            else
            {
                flags = new Flags();
            }
            AddMatch(regex, flags);
            AddAction(pattern, flags);
        }

        public void AddConditionFromParts(
            Pattern pattern,
            ParsedModRewriteInput input,
            Flags flags)
        {
            if (_conditions == null)
            {
                _conditions = new Conditions();
            }

            var condition = new Condition();

            condition.OrNext = flags.HasFlag(FlagType.Or);
            condition.Input = pattern;

            if (input.ConditionType == ConditionType.Regex)
            {
                // TODO make nullable?
                if (flags.HasFlag(FlagType.NoCase))
                {
                    condition.Match = new RegexMatch(new Regex(input.Operand, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout), input.Invert);
                }
                else
                {
                    condition.Match = new RegexMatch(new Regex(input.Operand, RegexOptions.Compiled, RegexTimeout), input.Invert);
                }
            }
            else if (input.ConditionType == ConditionType.IntComp)
            {
                switch (input.OperationType)
                {
                    case OperationType.Equal:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.Equal);
                        break;
                    case OperationType.Greater:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.Greater);
                        break;
                    case OperationType.GreaterEqual:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.GreaterEqual);
                        break;
                    case OperationType.Less:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.Less);
                        break;
                    case OperationType.LessEqual:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.LessEqual);
                        break;
                    case OperationType.NotEqual:
                        condition.Match = new IntegerMatch(input.Operand, IntegerOperationType.NotEqual);
                        break;
                    default:
                        throw new ArgumentException("Invalid operation for integer comparison.");
                }
            }
            else if (input.ConditionType == ConditionType.StringComp)
            {
                switch (input.OperationType)
                {
                    case OperationType.Equal:
                        condition.Match = new StringMatch(input.Operand, StringOperationType.Equal);
                        break;
                    case OperationType.Greater:
                        condition.Match = new StringMatch(input.Operand, StringOperationType.Greater);
                        break;
                    case OperationType.GreaterEqual:
                        condition.Match = new StringMatch(input.Operand, StringOperationType.GreaterEqual);
                        break;
                    case OperationType.Less:
                        condition.Match = new StringMatch(input.Operand, StringOperationType.Less);
                        break;
                    case OperationType.LessEqual:
                        condition.Match = new StringMatch(input.Operand, StringOperationType.LessEqual);
                        break;
                    default:
                        throw new ArgumentException("Invalid operation for string comparison.");
                }
            }
            else
            {
                switch (input.OperationType)
                {
                    case OperationType.Directory:
                        condition.Match = new IsDirectoryMatch(input.Invert);
                        break;
                    case OperationType.RegularFile:
                        condition.Match = new IsFileMatch(input.Invert);
                        break;
                    case OperationType.ExistingFile:
                        condition.Match = new IsFileMatch(input.Invert);
                        break;
                    case OperationType.SymbolicLink:
                        throw new NotImplementedException("Symbolic links are not implemented");
                    case OperationType.Size:
                        condition.Match = new FileSizeMatch(input.Invert);
                        break;
                    case OperationType.ExistingUrl:
                        throw new NotImplementedException("Existing Url lookups not implemented");
                    case OperationType.Executable:
                        throw new NotImplementedException("Executable Property search is not implemented");
                    default:
                        // TODO change exception
                        throw new ArgumentException("Invalid operation for property comparison.");
                }
            }
            _conditions.ConditionList.Add(condition);
        }

        public void AddMatch(
            ParsedModRewriteInput input,
            Flags flags)
        {
            if (flags.HasFlag(FlagType.NoCase))
            {
                _match = new RegexMatch(new Regex(input.Operand, RegexOptions.Compiled | RegexOptions.IgnoreCase, RegexTimeout), input.Invert);
            }
            else
            {
                _match = new RegexMatch(new Regex(input.Operand, RegexOptions.Compiled, RegexTimeout), input.Invert);
            }
        }

        public void AddAction(
            Pattern pattern,
            Flags flags)
        {
            // first create pre conditions
            if (_preActions == null)
            {
                _preActions = new List<PreAction>();
            }

            string flag;
            if (flags.GetValue(FlagType.Cookie, out flag))
            {
                // parse cookie
                _preActions.Add(new ChangeCookiePreAction(flag));
            }

            if (flags.GetValue(FlagType.Env, out flag))
            {
                // parse env
                _preActions.Add(new ChangeEnvironmentPreAction(flag));
            }

            if (flags.HasFlag(FlagType.Forbidden))
            {
                _action = new ForbiddenAction();
            }
            else if (flags.HasFlag(FlagType.Gone))
            {
                _action = new GoneAction();
            }
            else
            {
                var escapeBackReference = flags.HasFlag(FlagType.EscapeBackreference);
                var queryStringAppend = flags.HasFlag(FlagType.QSAppend);
                var queryStringDelete = flags.HasFlag(FlagType.QSDiscard);

                // is redirect?
                string statusCode;
                if (flags.GetValue(FlagType.Redirect, out statusCode))
                {
                    int res;
                    if (!int.TryParse(statusCode, out res))
                    {
                        throw new FormatException(Resources.FormatError_InputParserInvalidInteger(statusCode, -1));
                    }
                    _action = new ModRewriteRedirectAction(res, pattern, queryStringAppend, queryStringDelete, escapeBackReference);
                }
                else
                {
                    var last = flags.HasFlag(FlagType.End) || flags.HasFlag(FlagType.Last);
                    var redirect = last ? RuleResult.StopRules : RuleResult.Continue;
                    _action = new ModRewriteRewriteAction(redirect, pattern, queryStringAppend, queryStringDelete, escapeBackReference);
                }
            }
        }
    }
}
