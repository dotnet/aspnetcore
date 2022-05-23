// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite.Logging;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal sealed class IISUrlRewriteRule : IRule
{
    public string? Name { get; }
    public UrlMatch InitialMatch { get; }
    public ConditionCollection? Conditions { get; }
    public UrlAction Action { get; }
    public bool Global { get; }

    public IISUrlRewriteRule(string? name,
        UrlMatch initialMatch,
        ConditionCollection? conditions,
        UrlAction action)
        : this(name, initialMatch, conditions, action, false)
    {
    }

    public IISUrlRewriteRule(string? name,
        UrlMatch initialMatch,
        ConditionCollection? conditions,
        UrlAction action,
        bool global)
    {
        Name = name;
        InitialMatch = initialMatch;
        Conditions = conditions;
        Action = action;
        Global = global;
    }

    public void ApplyRule(RewriteContext context)
    {
        // Due to the path string always having a leading slash,
        // remove it from the path before regex comparison
        var path = context.HttpContext.Request.Path;
        MatchResults initMatchResults;
        if (path == PathString.Empty)
        {
            initMatchResults = InitialMatch.Evaluate(path.ToString(), context);
        }
        else
        {
            initMatchResults = InitialMatch.Evaluate(path.ToString().Substring(1), context);
        }

        if (!initMatchResults.Success)
        {
            context.Logger.UrlRewriteNotMatchedRule(Name);
            return;
        }

        MatchResults? condResult = null;
        if (Conditions != null)
        {
            condResult = ConditionEvaluator.Evaluate(Conditions, context, initMatchResults.BackReferences);
            if (!condResult.Success)
            {
                context.Logger.UrlRewriteNotMatchedRule(Name);
                return;
            }
        }

        context.Logger.UrlRewriteMatchedRule(Name);
        // at this point we know the rule passed, evaluate the replacement.
        Action.ApplyAction(context, initMatchResults?.BackReferences, condResult?.BackReferences);
    }
}
