// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.UrlActions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class RuleBuilder
    {
        private IList<Condition> _conditions;
        internal IList<UrlAction> _actions = new List<UrlAction>();
        private UrlMatch _match;
        private CookieActionFactory _cookieActionFactory = new CookieActionFactory();

        private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

        public ApacheModRewriteRule Build()
        {
            if (_actions.Count == 0 || _match == null)
            {
                throw new InvalidOperationException("Cannot create ModRewriteRule without action and match");
            }
            return new ApacheModRewriteRule(_match, _conditions, _actions);
        }

        public void AddRule(string rule)
        {
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
                _conditions = new List<Condition>();
            }

            var condition = new Condition();

            condition.OrNext = flags.HasFlag(FlagType.Or);
            condition.Input = pattern;

            switch (input.ConditionType)
            {
                case ConditionType.Regex:
                    if (flags.HasFlag(FlagType.NoCase))
                    {
                        condition.Match = new RegexMatch(new Regex(input.Operand, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout), input.Invert);
                    }
                    else
                    {
                        condition.Match = new RegexMatch(new Regex(input.Operand, RegexOptions.CultureInvariant | RegexOptions.Compiled, _regexTimeout), input.Invert);
                    }
                    break;
                case ConditionType.IntComp:
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
                    break;
                case ConditionType.StringComp:
                    switch (input.OperationType)
                    {
                        case OperationType.Equal:
                            condition.Match = new StringMatch(input.Operand, StringOperationType.Equal, input.Invert);
                            break;
                        case OperationType.Greater:
                            condition.Match = new StringMatch(input.Operand, StringOperationType.Greater, input.Invert);
                            break;
                        case OperationType.GreaterEqual:
                            condition.Match = new StringMatch(input.Operand, StringOperationType.GreaterEqual, input.Invert);
                            break;
                        case OperationType.Less:
                            condition.Match = new StringMatch(input.Operand, StringOperationType.Less, input.Invert);
                            break;
                        case OperationType.LessEqual:
                            condition.Match = new StringMatch(input.Operand, StringOperationType.LessEqual, input.Invert);
                            break;
                        default:
                            throw new ArgumentException("Invalid operation for string comparison.");
                    }
                    break;
                default:
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
                            // TODO see if FileAttributes.ReparsePoint works for this?
                            throw new NotImplementedException("Symbolic links are not supported because " +
                                                            "of cross platform implementation");
                        case OperationType.Size:
                            condition.Match = new FileSizeMatch(input.Invert);
                            break;
                        case OperationType.ExistingUrl:
                            throw new NotSupportedException("Existing Url lookups not supported because it requires a subrequest");
                        case OperationType.Executable:
                            throw new NotSupportedException("Executable Property is not supported because Windows " +
                                                            "requires a pinvoke to get this property");
                        default:
                            throw new ArgumentException("Invalid operation for property comparison");
                    }
                    break;
            }
            _conditions.Add(condition);
        }

        public void AddMatch(
            ParsedModRewriteInput input,
            Flags flags)
        {
            if (flags.HasFlag(FlagType.NoCase))
            {
                _match = new RegexMatch(new Regex(input.Operand, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout), input.Invert);
            }
            else
            {
                _match = new RegexMatch(new Regex(input.Operand, RegexOptions.CultureInvariant | RegexOptions.Compiled, _regexTimeout), input.Invert);
            }
        }

        public void AddAction(
            Pattern pattern,
            Flags flags)
        {
            string flag;
            if (flags.GetValue(FlagType.Cookie, out flag))
            {
                var action = _cookieActionFactory.Create(flag);
                _actions.Add(action);
            }

            if (flags.GetValue(FlagType.Env, out flag))
            {
                throw new NotSupportedException(Resources.Error_ChangeEnvironmentNotSupported);
            }

            if (flags.HasFlag(FlagType.Forbidden))
            {
                _actions.Add(new ForbiddenAction());
            }
            else if (flags.HasFlag(FlagType.Gone))
            {
                _actions.Add(new GoneAction());
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
                    int responseStatusCode;
                    if (string.IsNullOrEmpty(statusCode))
                    {
                        responseStatusCode = StatusCodes.Status302Found;
                    }
                    else if (!int.TryParse(statusCode, NumberStyles.None, CultureInfo.InvariantCulture, out responseStatusCode))
                    {
                        throw new FormatException(Resources.FormatError_InputParserInvalidInteger(statusCode, -1));
                    }
                    _actions.Add(new RedirectAction(responseStatusCode, pattern, queryStringAppend, queryStringDelete, escapeBackReference));
                }
                else
                {
                    var last = flags.HasFlag(FlagType.End) || flags.HasFlag(FlagType.Last);
                    var termination = last ? RuleResult.SkipRemainingRules : RuleResult.ContinueRules;
                    _actions.Add(new RewriteAction(termination, pattern, queryStringAppend, queryStringDelete, escapeBackReference));
                }
            }
        }
    }
}