// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public sealed class MatcherEndpoint : Endpoint
    {
        internal static readonly Func<RequestDelegate, RequestDelegate> EmptyInvoker = (next) =>
        {
            return (context) => Task.CompletedTask;
        };

        public MatcherEndpoint(
            Func<RequestDelegate, RequestDelegate> invoker,
            string template,
            RouteValueDictionary defaults,
            RouteValueDictionary requiredValues,
            List<MatchProcessorReference> nonInlineMatchProcessorReferences,
            int order,
            EndpointMetadataCollection metadata,
            string displayName)
            : base(metadata, displayName)
        {
            if (invoker == null)
            {
                throw new ArgumentNullException(nameof(invoker));
            }

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Invoker = invoker;
            Order = order;

            Template = template;
            ParsedTemplate = TemplateParser.Parse(template);

            RequiredValues = requiredValues;
            var mergedDefaults = GetDefaults(ParsedTemplate, defaults);
            Defaults = mergedDefaults;

            var mergedReferences = MergeMatchProcessorReferences(ParsedTemplate, nonInlineMatchProcessorReferences);
            MatchProcessorReferences = mergedReferences.AsReadOnly();
        }

        public int Order { get; }
        public Func<RequestDelegate, RequestDelegate> Invoker { get; }
        public string Template { get; }
        public RouteValueDictionary Defaults { get; }

        // Values required by an endpoint for it to be successfully matched on link generation
        public RouteValueDictionary RequiredValues { get; }

        // Todo: needs review
        public RouteTemplate ParsedTemplate { get; }

        public IReadOnlyList<MatchProcessorReference> MatchProcessorReferences { get; }

        // Merge inline and non inline defaults into one
        private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate, RouteValueDictionary nonInlineDefaults)
        {
            var result = nonInlineDefaults == null ? new RouteValueDictionary() : new RouteValueDictionary(nonInlineDefaults);

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    if (result.ContainsKey(parameter.Name))
                    {
                        throw new InvalidOperationException(
                          Resources.FormatTemplateRoute_CannotHaveDefaultValueSpecifiedInlineAndExplicitly(
                              parameter.Name));
                    }
                    else
                    {
                        result.Add(parameter.Name, parameter.DefaultValue);
                    }
                }
            }

            return result;
        }

        private List<MatchProcessorReference> MergeMatchProcessorReferences(
            RouteTemplate parsedTemplate,
            List<MatchProcessorReference> nonInlineReferences)
        {
            var matchProcessorReferences = new List<MatchProcessorReference>();

            if (nonInlineReferences != null)
            {
                matchProcessorReferences.AddRange(nonInlineReferences);
            }

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.InlineConstraints != null)
                {
                    foreach (var constraint in parameter.InlineConstraints)
                    {
                        matchProcessorReferences.Add(
                            new MatchProcessorReference(
                                parameter.Name,
                                optional: parameter.IsOptional,
                                constraintText: constraint.Constraint));
                    }
                }
            }
            return matchProcessorReferences;
        }
    }
}
