// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorsCollectionProvider;
        private readonly IActionSelectorDecisionTreeProvider _decisionTreeProvider;
        private ILogger _logger;

        public DefaultActionSelector(
            [NotNull] IActionDescriptorsCollectionProvider actionDescriptorsCollectionProvider,
            [NotNull] IActionSelectorDecisionTreeProvider decisionTreeProvider,
            [NotNull] ILoggerFactory loggerFactory)
        {
            _actionDescriptorsCollectionProvider = actionDescriptorsCollectionProvider;
            _decisionTreeProvider = decisionTreeProvider;
            _logger = loggerFactory.Create<DefaultActionSelector>();
        }

        public Task<ActionDescriptor> SelectAsync([NotNull] RouteContext context)
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

                var finalMatches = SelectBestActions(matching);

                if (finalMatches.Count == 0)
                {
                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingRouteAndMethodConstraints = matchingRouteAndMethodConstraints,
                            ActionsMatchingRouteAndMethodAndDynamicConstraints =
                                matchingRouteAndMethodAndDynamicConstraints,
                            FinalMatches = finalMatches,
                        });
                    }

                    return Task.FromResult<ActionDescriptor>(null);
                }
                else if (finalMatches.Count == 1)
                {
                    var selectedAction = finalMatches[0];

                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingRouteAndMethodConstraints = matchingRouteAndMethodConstraints,
                            ActionsMatchingRouteAndMethodAndDynamicConstraints = 
                                matchingRouteAndMethodAndDynamicConstraints,
                            FinalMatches = finalMatches,
                            SelectedAction = selectedAction
                        });
                    }

                    return Task.FromResult(selectedAction);
                }
                else
                {
                    if (_logger.IsEnabled(TraceType.Information))
                    {
                        _logger.WriteValues(new DefaultActionSelectorSelectAsyncValues()
                        {
                            ActionsMatchingRouteConstraints = matchingRouteConstraints,
                            ActionsMatchingRouteAndMethodConstraints = matchingRouteAndMethodConstraints,
                            ActionsMatchingRouteAndMethodAndDynamicConstraints =
                                matchingRouteAndMethodAndDynamicConstraints,
                            FinalMatches = finalMatches,
                        });
                    }

                    var actionNames = string.Join(
                        Environment.NewLine,
                        finalMatches.Select(a => a.DisplayName));

                    var message = Resources.FormatDefaultActionSelector_AmbiguousActions(
                        Environment.NewLine,
                        actionNames);

                    throw new AmbiguousActionException(message);
                }
            }
        }

        /// <summary>
        /// Returns the set of best matching actions.
        /// </summary>
        /// <param name="actions">The set of actions that satisfy all constraints.</param>
        /// <returns>A list of the best matching actions.</returns>
        protected virtual IReadOnlyList<ActionDescriptor> SelectBestActions(IReadOnlyList<ActionDescriptor> actions)
        {
            return actions;
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
    }
}
