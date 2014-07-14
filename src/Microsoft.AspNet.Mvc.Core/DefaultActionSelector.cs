// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorsCollectionProvider;
        private readonly IActionSelectorDecisionTreeProvider _decisionTreeProvider;
        private readonly IActionBindingContextProvider _bindingProvider;
        private ILogger _logger;

        public DefaultActionSelector(
            [NotNull] IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider,
            [NotNull] IActionSelectorDecisionTreeProvider decisionTreeProvider, 
            [NotNull] IActionBindingContextProvider bindingProvider,
            [NotNull] ILoggerFactory loggerFactory)
        {
            _actionDescriptorsCollectionProvider = actionDescriptorsCollectionProvider;
            _decisionTreeProvider = decisionTreeProvider;
            _bindingProvider = bindingProvider;
            _logger = loggerFactory.Create<DefaultActionSelector>();
        }

        public async Task<ActionDescriptor> SelectAsync([NotNull] RouteContext context)
        {
            using (_logger.BeginScope("DefaultActionSelector.SelectAsync"))
            {
                var tree = _decisionTreeProvider.DecisionTree;
                var matchingRouteConstraints = tree.Select(context.RouteData.Values);

                var matchingRouteAndMethodConstraints =
                    matchingRouteConstraints.Where(ad =>
                        MatchMethodConstraints(ad, context)).ToList();

                var matchingRouteAndMethodAndDynamicConstraints =
                    matchingRouteAndMethodConstraints.Where(ad =>
                        MatchDynamicConstraints(ad, context)).ToList();

                var matching = matchingRouteAndMethodAndDynamicConstraints;

                var matchesWithConstraints = new List<ActionDescriptor>();
                foreach (var match in matching)
                {
                    if (match.DynamicConstraints != null && match.DynamicConstraints.Any() ||
                        match.MethodConstraints != null && match.MethodConstraints.Any())
                    {
                        matchesWithConstraints.Add(match);
                    }
                }

                // If any action that's applicable has constraints, this is considered better than 
                // an action without.
                if (matchesWithConstraints.Any())
                {
                    matching = matchesWithConstraints;
                }

                if (matching.Count == 0)
                {
                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingRouteAndMethodConstraints = matchingRouteAndMethodConstraints,
                            ActionsMatchingRouteAndMethodAndDynamicConstraints = 
                                matchingRouteAndMethodAndDynamicConstraints,
                            ActionsMatchingWithConstraints = matchesWithConstraints
                        });
                    }

                    return null;
                }
                else
                {
                    var selectedAction = await SelectBestCandidate(context, matching);

                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingRouteAndMethodConstraints = matchingRouteAndMethodConstraints,
                            ActionsMatchingRouteAndMethodAndDynamicConstraints = 
                                matchingRouteAndMethodAndDynamicConstraints,
                            ActionsMatchingWithConstraints = matchesWithConstraints,
                            SelectedAction = selectedAction
                        });
                    }

                    return selectedAction;
                }
            }
        }

        private bool MatchMethodConstraints(ActionDescriptor descriptor, RouteContext context)
        {
            return descriptor.MethodConstraints == null ||
                    descriptor.MethodConstraints.All(c => c.Accept(context));
        }

        private bool MatchDynamicConstraints(ActionDescriptor descriptor, RouteContext context)
        {
            return descriptor.DynamicConstraints == null ||
                    descriptor.DynamicConstraints.All(c => c.Accept(context));
        }

        protected virtual async Task<ActionDescriptor> SelectBestCandidate(
            RouteContext context,
            List<ActionDescriptor> candidates)
        {
            var applicableCandiates = new List<ActionDescriptorCandidate>();
            foreach (var action in candidates)
            {
                var isApplicable = true;
                var candidate = new ActionDescriptorCandidate()
                {
                    Action = action,
                };

                var actionContext = new ActionContext(context, action);
                var actionBindingContext = await _bindingProvider.GetActionBindingContextAsync(actionContext);

                foreach (var parameter in action.Parameters.Where(p => p.ParameterBindingInfo != null))
                {
                    if (!ValueProviderResult.CanConvertFromString(parameter.ParameterBindingInfo.ParameterType))
                    {
                        continue;
                    }

                    if (await actionBindingContext.ValueProvider.ContainsPrefixAsync(
                        parameter.ParameterBindingInfo.Prefix))
                    {
                        candidate.FoundParameters++;
                        if (parameter.IsOptional)
                        {
                            candidate.FoundOptionalParameters++;
                        }
                    }
                    else if (!parameter.IsOptional)
                    {
                        isApplicable = false;
                        break;
                    }
                }

                if (isApplicable)
                {
                    applicableCandiates.Add(candidate);
                }
            }

            if (applicableCandiates.Count == 0)
            {
                return null;
            }

            var mostParametersSatisfied =
                applicableCandiates
                .GroupBy(c => c.FoundParameters)
                .OrderByDescending(g => g.Key)
                .First();

            var fewestOptionalParameters =
                mostParametersSatisfied
                .GroupBy(c => c.FoundOptionalParameters)
                .OrderBy(g => g.Key).First()
                .ToArray();

            if (fewestOptionalParameters.Length > 1)
            {
                throw new InvalidOperationException("The actions are ambiguious.");
            }

            return fewestOptionalParameters[0].Action;
        }

        // This method attempts to ensure that the route that's about to generate a link will generate a link
        // to an existing action. This method is called by a route (through MvcApplication) prior to generating
        // any link - this gives WebFX a chance to 'veto' the values provided by a route.
        //
        // This method does not take httpmethod or dynamic action constraints into account.
        public virtual bool HasValidAction([NotNull] VirtualPathContext context)
        {
            if (context.ProvidedValues == null)
            {
                // We need the route's values to be able to double check our work.
                return false;
            }

            var actions =
                GetActions().Where(
                    action =>
                        action.RouteConstraints == null ||
                        action.RouteConstraints.All(constraint => constraint.Accept(context.ProvidedValues)));

            return actions.Any();
        }

        private IReadOnlyList<ActionDescriptor> GetActions()
        {
            var descriptors = _actionDescriptorsCollectionProvider.ActionDescriptors;

            if (descriptors == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatPropertyOfTypeCannotBeNull("ActionDescriptors",
                                                               _actionDescriptorsCollectionProvider.GetType()));
            }

            return descriptors.Items;
        }

        private class ActionDescriptorCandidate
        {
            public ActionDescriptor Action { get; set; }

            public int FoundParameters { get; set; }

            public int FoundOptionalParameters { get; set; }
        }
    }
}
