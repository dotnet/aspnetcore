// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class ModRewriteRule : Rule
    {
        public List<Condition> Conditions { get; set; } = new List<Condition>();
        public string Description { get; set; } = string.Empty;
        public RuleExpression InitialRule { get; set; }
        public Pattern Transform { get; set; }
        public RuleFlags Flags { get; set; } = new RuleFlags();
        public ModRewriteRule() { }

        public ModRewriteRule(List<Condition> conditions, RuleExpression initialRule, Pattern transforms, RuleFlags flags, string description = "")
        {
            Conditions = conditions;
            InitialRule = initialRule;
            Transform = transforms;
            Flags = flags;
            Description = description;
        }

        public override RuleResult ApplyRule(RewriteContext context)
        {
            // 1. Figure out which section of the string to match for the initial rule.
            var results = InitialRule.Operand.RegexOperation.Match(context.HttpContext.Request.Path.ToString());

            string flagRes = null;
            if (CheckMatchResult(results.Success))
            {
                return RuleResult.Continue;
            }

            if (Flags.HasFlag(RuleFlagType.EscapeBackreference))
            {
                // TODO Escape Backreferences here.
            }

            // 2. Go through all conditions and compare them to the created string
            var previous = Match.Empty;

            if (!CheckCondition(context, results, previous))
            {
                return RuleResult.Continue;
            }
            // TODO add chained flag

            // at this point, our rule passed, we can now apply the on match function
            var result = Transform.GetPattern(context.HttpContext, results, previous);

            if (Flags.HasFlag(RuleFlagType.QSDiscard))
            {
                context.HttpContext.Request.QueryString = new QueryString();
            }

            if ((flagRes = Flags.GetValue(RuleFlagType.Cookie)) != null)
            {
                // TODO CreateCookies(context);
                // context.HttpContext.Response.Cookies.Append()
                // Make sure this in on compile.
            }

            if ((flagRes = Flags.GetValue(RuleFlagType.Env)) != null)
            {
                // TODO CreateEnv(context)
                // context.HttpContext...
            }

            if ((flagRes = Flags.GetValue(RuleFlagType.Next)) != null)
            {
                // TODO Next flag
            }

            if (Flags.HasFlag(RuleFlagType.Forbidden))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return RuleResult.ResponseComplete;
            }
            else if (Flags.HasFlag(RuleFlagType.Gone))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status410Gone;
                return RuleResult.ResponseComplete;
            }
            else if (result == "-")
            {
                // TODO set url to result.
            }
            else if (Flags.HasFlag(RuleFlagType.QSAppend))
            {
                context.HttpContext.Request.QueryString = context.HttpContext.Request.QueryString.Add(new QueryString(result));
            }

            if ((flagRes = Flags.GetValue(RuleFlagType.Redirect)) != null)
            {
                int parsedInt;
                if (!int.TryParse(flagRes, out parsedInt))
                {
                    // TODO PERF parse the status code when the flag is parsed rather than per request
                    throw new FormatException("Trying to parse non-int in integer comparison.");
                }
                context.HttpContext.Response.StatusCode = parsedInt;
                if (Flags.HasFlag(RuleFlagType.FullUrl))
                {
                    // TODO review escaping
                    context.HttpContext.Response.Headers[HeaderNames.Location] = result;
                }
                else
                {
                    // TODO str cat is bad, polish, review escaping
                    if (result.StartsWith("/"))
                    {
                        context.HttpContext.Response.Headers[HeaderNames.Location] = result + context.HttpContext.Request.QueryString;
                    }
                    else
                    {
                        context.HttpContext.Response.Headers[HeaderNames.Location] = "/" + result + context.HttpContext.Request.QueryString;
                    }
                }
                return RuleResult.ResponseComplete;
            }
            else
            {
                if (Flags.HasFlag(RuleFlagType.FullUrl))
                {
                    ModifyHttpContextFromUri(context.HttpContext, result);
                }
                else
                {
                    if (result.StartsWith("/"))
                    {
                        context.HttpContext.Request.Path = new PathString(result);
                    }
                    else
                    {
                        context.HttpContext.Request.Path = new PathString("/" + result);
                    }
                }
                if (Flags.HasFlag(RuleFlagType.Last) || Flags.HasFlag(RuleFlagType.End))
                {
                    return RuleResult.StopRules;
                }
                else
                {
                    return RuleResult.Continue;
                }
            }
        }

        private bool CheckMatchResult(bool? result)
        {
            if (result == null)
            {
                return false;
            }
            return !(result.Value ^ InitialRule.Invert);
        }

        private bool CheckCondition(RewriteContext context, Match results, Match previous)
        {
            if (Conditions == null)
            {
                return true;
            }

            // TODO Visitor pattern here?
            foreach (var condition in Conditions)
            {
                var concatTestString = condition.TestStringSegments.GetPattern(context.HttpContext, results, previous);
                var match = condition.ConditionExpression.CheckConditionExpression(context, previous, concatTestString);

                if (match == null)
                {
                    return false;
                }

                if (!match.Value && !(condition.Flags.HasFlag(ConditionFlagType.Or)))
                {
                    return false;
                }
            }
            return true;
        }

        private void ModifyHttpContextFromUri(HttpContext context, string uriString)
        {
            var uri = new Uri(uriString);
            // TODO this is ugly, fix in later push.
            // TODO super bad for perf, cache/locally store these and update httpcontext after all rules are applied.
            var pathBase = PathString.FromUriComponent(uri);
            if (!pathBase.Value.StartsWith(context.Request.PathBase))
            {
                // cannot distinguish between path base and path.
                throw new NotSupportedException("Modified path base from mod_rewrite rule");
            }
            context.Request.Host = HostString.FromUriComponent(uri);
            context.Request.Path = PathString.FromUriComponent(uri);
            context.Request.QueryString = QueryString.FromUriComponent(uri);
            context.Request.Scheme = uri.Scheme;
        }
    }
}
