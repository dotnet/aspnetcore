// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing.Template;

internal sealed class DefaultTemplateBinderFactory : TemplateBinderFactory
{
    private readonly ParameterPolicyFactory _policyFactory;
    private readonly ObjectPool<UriBuildingContext> _pool;

    public DefaultTemplateBinderFactory(
        ParameterPolicyFactory policyFactory,
        ObjectPool<UriBuildingContext> pool)
    {
        ArgumentNullException.ThrowIfNull(policyFactory);
        ArgumentNullException.ThrowIfNull(pool);

        _policyFactory = policyFactory;
        _pool = pool;
    }

    public override TemplateBinder Create(RouteTemplate template, RouteValueDictionary defaults)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(defaults);

        return new TemplateBinder(UrlEncoder.Default, _pool, template, defaults);
    }

    public override TemplateBinder Create(RoutePattern pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        // Now create the constraints and parameter transformers from the pattern
        var policies = new List<(string parameterName, IParameterPolicy policy)>();
        foreach (var kvp in pattern.ParameterPolicies)
        {
            var parameterName = kvp.Key;

            // It's possible that we don't have an actual route parameter, we need to support that case.
            var parameter = pattern.GetParameter(parameterName);

            // Use the first parameter transformer per parameter
            var foundTransformer = false;
            for (var i = 0; i < kvp.Value.Count; i++)
            {
                var parameterPolicy = _policyFactory.Create(parameter, kvp.Value[i]);
                if (!foundTransformer && parameterPolicy is IOutboundParameterTransformer parameterTransformer)
                {
                    policies.Add((parameterName, parameterTransformer));
                    foundTransformer = true;
                }

                if (parameterPolicy is IRouteConstraint constraint)
                {
                    policies.Add((parameterName, constraint));
                }
            }
        }

        return new TemplateBinder(UrlEncoder.Default, _pool, pattern, policies);
    }
}
