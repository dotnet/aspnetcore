// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;

namespace Microsoft.AspNetCore.Components.Forms;

internal class HtmlFieldPrefix(LambdaExpression initial)
{
    private readonly LambdaExpression[] _rest = Array.Empty<LambdaExpression>();

    internal HtmlFieldPrefix(LambdaExpression expression, params LambdaExpression[] rest)
        : this(expression)
    {
        _rest = rest;
    }

    public HtmlFieldPrefix Combine(LambdaExpression other)
    {
        var restLength = _rest?.Length ?? 0;
        var length = restLength + 1;
        var expressions = new LambdaExpression[length];
        for (var i = 0; i < restLength - 1; i++)
        {
            expressions[i] = _rest![i];
        }

        expressions[length - 1] = other;

        return new HtmlFieldPrefix(initial, expressions);
    }

    public string GetFieldName(LambdaExpression expression)
    {
        var prefix = ExpressionFormatter.FormatLambda(initial);
        var restLength = _rest?.Length ?? 0;
        for (var i = 0; i < restLength; i++)
        {
            prefix = ExpressionFormatter.FormatLambda(_rest![i], prefix);
        }

        return ExpressionFormatter.FormatLambda(expression, prefix);
    }
}
