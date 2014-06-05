// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly INestedProviderManager<ActionDescriptorProviderContext> _actionDescriptorProvider;
        private readonly IActionBindingContextProvider _bindingProvider;

        public DefaultActionSelector(INestedProviderManager<ActionDescriptorProviderContext> actionDescriptorProvider,
                                     IActionBindingContextProvider bindingProvider)
        {
            _actionDescriptorProvider = actionDescriptorProvider;
            _bindingProvider = bindingProvider;
        }

        public async Task<ActionDescriptor> SelectAsync([NotNull] RouteContext context)
        {
            var allDescriptors = GetActions();

            var matching = allDescriptors.Where(ad => Match(ad, context)).ToList();

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
                return null;
            }
            else
            {
                return await SelectBestCandidate(context, matching);
            }
        }

        public bool Match(ActionDescriptor descriptor, RouteContext context)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            return (descriptor.RouteConstraints == null || descriptor.RouteConstraints.All(c => c.Accept(context))) &&
                   (descriptor.MethodConstraints == null || descriptor.MethodConstraints.All(c => c.Accept(context))) &&
                   (descriptor.DynamicConstraints == null || descriptor.DynamicConstraints.All(c => c.Accept(context)));
        }

        protected virtual async Task<ActionDescriptor> SelectBestCandidate(RouteContext context, List<ActionDescriptor> candidates)
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

                    if (await actionBindingContext.ValueProvider.ContainsPrefixAsync(parameter.ParameterBindingInfo.Prefix))
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

        // This is called by the default UrlHelper as part of Action link generation. When a link is requested
        // specifically for an Action, we manipulate the route data to ensure that the right link is generated.
        // Read further for details.
        public virtual IEnumerable<ActionDescriptor> GetCandidateActions(VirtualPathContext context)
        {
            // This method attemptss to find a unique 'best' candidate set of actions from the provided route
            // values and ambient route values.
            //
            // The purpose of this process is to avoid allowing certain routes to be too greedy. When a route uses 
            // a default value as a filter, it can generate links to actions it will never hit. The actions returned 
            // by this method are used by the link generation code to manipulate the route values so that routes that 
            // are are greedy can't generate a link.
            //
            // The best example of this greediness is the canonical 'area' route from MVC.
            //
            // Ex: Areas/Admin/{controller}/{action} (defaults { area = "Admin" })
            //
            // This route can generate a link even when the 'area' token is not provided.
            //
            //
            // We define 'best' based on the combination of Values and AmbientValues. This set can be used to select a 
            // set of actions, anything in this is set is 'reachable'. We determine 'best' by looking for the 'reachable'
            // actions ordered by the most total constraints matched, then the most constraints matched by ambient values.
            //
            // Ex: 
            //      Consider the following actions - Home/Index (no area), and Admin/Home/Index (area = Admin).
            //      ambient values = { area = "Admin", controller = "Home", action = "Diagnostics" }
            //      values = { action = "Index" }
            //
            //      In this case we want to select the Admin/Home/Index action, and algorithm leads us there.
            //
            //      Admin/Home/Index: Total score 3, Explicit score 2, Implicit score 1, Omission score 0
            //      Home/Index: Total score 3, Explicit score 2, Implicit score 0, Omission score 1
            //
            // The description here is based on the concepts we're using to implement areas in WebFx, but apply
            // to any tokens that might be used in routing (including REST conventions when action == null).
            // 
            // This method does not take httpmethod or dynamic action constraints into account.

            var actions = GetActions();

            var candidates = new List<ActionDescriptorLinkCandidate>();
            foreach (var action in actions)
            {
                var candidate = new ActionDescriptorLinkCandidate() { Action = action };
                if (action.RouteConstraints == null)
                {
                    candidates.Add(candidate);
                    continue;
                }

                bool isActionValid = true;
                foreach (var constraint in action.RouteConstraints)
                {
                    if (constraint.Accept(context.Values))
                    {
                        if (context.Values.ContainsKey(constraint.RouteKey))
                        {
                            // Explicit value is acceptable
                            candidate.ExplicitMatches++;
                        }
                        else
                        {
                            // No value supplied and that's OK for this action.
                            candidate.OmissionMatches++;
                        }
                    }
                    else if (context.Values.ContainsKey(constraint.RouteKey))
                    {
                        // There's an explicitly provided value, but the action constraint doesn't match it.
                        isActionValid = false;
                        break;
                    }
                    else if (constraint.Accept(context.AmbientValues))
                    {
                        // Ambient value is acceptable, used as a fallback
                        candidate.ImplicitMatches++;
                    }
                    else
                    {
                        // No possible match
                        isActionValid = false;
                        break;
                    }
                }

                if (isActionValid)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return Enumerable.Empty<ActionDescriptor>();
            }

            // Finds all of the actions with the maximum number of total constraint matches.
            var longestMatches =
                candidates
                .GroupBy(c => c.TotalMatches)
                .OrderByDescending(g => g.Key)
                .First();

            // Finds all of the actions (from the above set) with the maximum number of explicit constraint matches.
            var bestMatchesByExplicit =
                longestMatches
                .GroupBy(c => c.ExplicitMatches)
                .OrderByDescending(g => g.Key)
                .First();

            // Finds all of the actions (from the above set) with the maximum number of implicit constraint matches.
            var bestMatchesByImplicit =
                bestMatchesByExplicit
                .GroupBy(c => c.ImplicitMatches)
                .OrderByDescending(g => g.Key)
                .First();

            var bestActions = bestMatchesByImplicit.Select(m => m.Action).ToArray();
            if (bestActions.Length == 1)
            {
                return bestActions;
            }

            var exemplar = FindEquivalenceClass(bestActions);
            if (exemplar == null)
            {
                throw new InvalidOperationException(Resources.ActionSelector_GetCandidateActionsIsAmbiguous);
            }
            else
            {
                return bestActions;
            }
        }

        // This method determines if the set of action descriptor candidates share a common set
        // of route constraints, and returns an exemplar if there's a single set. This identifies
        // a type of ambiguity, more data must be specified to ensure the right action can be selected.
        //
        // This is a no-op for our default conventions, but becomes important with custom action
        // descriptor providers.
        // 
        // Ex: These are not in the same equivalence class.
        //  Action 1: constraint keys - { action, controller, area }
        //  Action 2: constraint keys - { action, module }
        private ActionDescriptor FindEquivalenceClass(ActionDescriptor[] candidates)
        {
            Contract.Assert(candidates.Length > 1);

            var criteria = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var exemplar = candidates[0];
            foreach (var constraint in exemplar.RouteConstraints)
            {
                criteria.Add(constraint.RouteKey);
            }

            for (var i = 1; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                foreach (var constraint in exemplar.RouteConstraints)
                {
                    if (criteria.Add(constraint.RouteKey))
                    {
                        // This is a new criterion - the candidates have multiple criteria sets
                        return null;
                    }
                }
            }

            return exemplar;
        }

        private List<ActionDescriptor> GetActions()
        {
            var actionDescriptorProviderContext = new ActionDescriptorProviderContext();
            _actionDescriptorProvider.Invoke(actionDescriptorProviderContext);

            return actionDescriptorProviderContext.Results;
        }

        private class ActionDescriptorCandidate
        {
            public ActionDescriptor Action { get; set; }

            public int FoundParameters { get; set; }

            public int FoundOptionalParameters { get; set; }
        }

        private class ActionDescriptorLinkCandidate
        {
            public ActionDescriptor Action { get; set; }

            // Matches from explicit route values
            public int ExplicitMatches { get; set; }

            // Matches from ambient route values
            public int ImplicitMatches { get; set; }

            // Matches from explicit route values (by omission)
            public int OmissionMatches { get; set; }

            public int TotalMatches
            {
                get { return ExplicitMatches + ImplicitMatches + OmissionMatches; }
            }
        }
    }
}
