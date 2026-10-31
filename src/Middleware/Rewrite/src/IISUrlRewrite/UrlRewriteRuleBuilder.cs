// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.UrlMatches;

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal sealed class UrlRewriteRuleBuilder
{
    private readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    public string? Name { get; set; }
    public bool Enabled { get; set; }
    public bool Global { get; set; }
    public UriMatchPart UriMatchPart => Global ? UriMatchPart.Full : UriMatchPart.Path;

    private UrlMatch? _initialMatch;
    private ConditionCollection? _conditions;
    private UrlAction? _action;

    public IISUrlRewriteRule Build()
    {
        if (_initialMatch == null || _action == null)
        {
            throw new InvalidOperationException("Cannot create UrlRewriteRule without action and match");
        }

        return new IISUrlRewriteRule(Name, _initialMatch, _conditions, _action, Global);
    }

    public void AddUrlAction(UrlAction action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action), "Rules must contain an action");
        }
        _action = action;
    }

    public void AddUrlMatch(string input, bool ignoreCase = true, bool negate = false, PatternSyntax patternSyntax = PatternSyntax.ECMAScript)
    {
        switch (patternSyntax)
        {
            case PatternSyntax.ECMAScript:
                {
                    if (ignoreCase)
                    {
                        var regex = new Regex(input, RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase, _regexTimeout);
                        _initialMatch = new RegexMatch(regex, negate);
                    }
                    else
                    {
                        var regex = new Regex(input, RegexOptions.CultureInvariant | RegexOptions.Compiled, _regexTimeout);
                        _initialMatch = new RegexMatch(regex, negate);
                    }
                    break;
                }
            case PatternSyntax.Wildcard:
                throw new NotSupportedException("Wildcard syntax is not supported");
            case PatternSyntax.ExactMatch:
                _initialMatch = new ExactMatch(ignoreCase, input, negate);
                break;
        }
    }

    public void ConfigureConditionBehavior(LogicalGrouping logicalGrouping, bool trackAllCaptures)
    {
        _conditions = new ConditionCollection(logicalGrouping, trackAllCaptures);
    }

    public void AddUrlCondition(Condition condition)
    {
        if (_conditions == null)
        {
            throw new InvalidOperationException($"You must first configure condition behavior by calling {nameof(ConfigureConditionBehavior)}");
        }
        ArgumentNullException.ThrowIfNull(condition);
        _conditions.Add(condition);
    }

    public void AddUrlConditions(IEnumerable<Condition> conditions)
    {
        if (_conditions == null)
        {
            throw new InvalidOperationException($"You must first configure condition behavior by calling {nameof(ConfigureConditionBehavior)}");
        }
        _conditions.AddConditions(conditions);
    }
}
