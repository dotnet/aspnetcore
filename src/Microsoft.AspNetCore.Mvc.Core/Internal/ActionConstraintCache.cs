// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ActionConstraintCache
    {
        private readonly IActionDescriptorCollectionProvider _collectionProvider;
        private readonly IActionConstraintProvider[] _actionConstraintProviders;

        private volatile InnerCache _currentCache;

        public ActionConstraintCache(
            IActionDescriptorCollectionProvider collectionProvider,
            IEnumerable<IActionConstraintProvider> actionConstraintProviders)
        {
            _collectionProvider = collectionProvider;
            _actionConstraintProviders = actionConstraintProviders.OrderBy(item => item.Order).ToArray();
        }

        internal InnerCache CurrentCache
        {
            get
            {
                var current = _currentCache;
                var actionDescriptors = _collectionProvider.ActionDescriptors;

                if (current == null || current.Version != actionDescriptors.Version)
                {
                    current = new InnerCache(actionDescriptors);
                    _currentCache = current;
                }

                return current;
            }
        }

        public IReadOnlyList<IActionConstraint> GetActionConstraints(HttpContext httpContext, ActionDescriptor action)
        {
            var cache = CurrentCache;

            if (cache.Entries.TryGetValue(action, out var entry))
            {
                return GetActionConstraintsFromEntry(entry, httpContext, action);
            }

            if (action.ActionConstraints == null || action.ActionConstraints.Count == 0)
            {
                return null;
            }

            var items = new List<ActionConstraintItem>(action.ActionConstraints.Count);
            for (var i = 0; i < action.ActionConstraints.Count; i++)
            {
                items.Add(new ActionConstraintItem(action.ActionConstraints[i]));
            }

            ExecuteProviders(httpContext, action, items);

            var actionConstraints = ExtractActionConstraints(items);

            var allActionConstraintsCached = true;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.IsReusable)
                {
                    item.Constraint = null;
                    allActionConstraintsCached = false;
                }
            }

            if (allActionConstraintsCached)
            {
                entry = new CacheEntry(actionConstraints);
            }
            else
            {
                entry = new CacheEntry(items);
            }

            cache.Entries.TryAdd(action, entry);
            return actionConstraints;
        }

        private IReadOnlyList<IActionConstraint> GetActionConstraintsFromEntry(CacheEntry entry, HttpContext httpContext, ActionDescriptor action)
        {
            Debug.Assert(entry.ActionConstraints != null || entry.Items != null);

            if (entry.ActionConstraints != null)
            {
                return entry.ActionConstraints;
            }

            var items = new List<ActionConstraintItem>(entry.Items.Count);
            for (var i = 0; i < entry.Items.Count; i++)
            {
                var item = entry.Items[i];
                if (item.IsReusable)
                {
                    items.Add(item);
                }
                else
                {
                    items.Add(new ActionConstraintItem(item.Metadata));
                }
            }

            ExecuteProviders(httpContext, action, items);

            return ExtractActionConstraints(items);
        }

        private void ExecuteProviders(HttpContext httpContext, ActionDescriptor action, List<ActionConstraintItem> items)
        {
            var context = new ActionConstraintProviderContext(httpContext, action, items);

            for (var i = 0; i < _actionConstraintProviders.Length; i++)
            {
                _actionConstraintProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _actionConstraintProviders.Length - 1; i >= 0; i--)
            {
                _actionConstraintProviders[i].OnProvidersExecuted(context);
            }
        }

        private IReadOnlyList<IActionConstraint> ExtractActionConstraints(List<ActionConstraintItem> items)
        {
            var count = 0;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Constraint != null)
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return null;
            }
            
            var actionConstraints = new IActionConstraint[count];
            var actionConstraintIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var actionConstraint = items[i].Constraint;
                if (actionConstraint != null)
                {
                    actionConstraints[actionConstraintIndex++] = actionConstraint;
                }
            }

            return actionConstraints;
        }

        internal class InnerCache
        {
            private readonly ActionDescriptorCollection _actions;
            private bool? _hasActionConstraints;

            public InnerCache(ActionDescriptorCollection actions)
            {
                _actions = actions;
            }

            public ConcurrentDictionary<ActionDescriptor, CacheEntry> Entries { get; } =
                new ConcurrentDictionary<ActionDescriptor, CacheEntry>();

            public int Version => _actions.Version;

            public bool HasActionConstraints
            {
                get
                {
                    // This is a safe race-condition, since it always transitions from null to non-null.
                    // All writers will always get the same result.
                    if (_hasActionConstraints == null)
                    {
                        var found = false;
                        for (var i = 0; i < _actions.Items.Count; i++)
                        {
                            var action = _actions.Items[i];
                            if (action.ActionConstraints?.Count > 0 && HasSignificantActionConstraint(action))
                            {
                                // We need to check for some specific action constraint implementations.
                                // We've implemented consumes, and HTTP method support inside endpoint routing, so 
                                // we don't need to run an 'action constraint phase' if those are the only constraints.
                                found = true;
                                break;
                            }
                        }

                        _hasActionConstraints = found;

                        bool HasSignificantActionConstraint(ActionDescriptor action)
                        {
                            for (var i = 0; i < action.ActionConstraints.Count; i++)
                            {
                                var actionConstraint = action.ActionConstraints[i];
                                if (actionConstraint.GetType() == typeof(HttpMethodActionConstraint))
                                {
                                    // This one is OK, we implement this in endpoint routing.
                                }
                                else if (actionConstraint.GetType().FullName == "Microsoft.AspNetCore.Mvc.Cors.Internal.CorsHttpMethodActionConstraint")
                                {
                                    // This one is OK, we implement this in endpoint routing.
                                }
                                else if (actionConstraint.GetType() == typeof(ConsumesAttribute))
                                {
                                    // This one is OK, we implement this in endpoint routing.
                                }
                                else
                                {
                                    return true;
                                }
                            }

                            return false;
                        }
                    }

                    return _hasActionConstraints.Value;
                }
            }
        }

        internal readonly struct CacheEntry
        {
            public CacheEntry(IReadOnlyList<IActionConstraint> actionConstraints)
            {
                ActionConstraints = actionConstraints;
                Items = null;
            }

            public CacheEntry(List<ActionConstraintItem> items)
            {
                Items = items;
                ActionConstraints = null;
            }

            public IReadOnlyList<IActionConstraint> ActionConstraints { get; }

            public List<ActionConstraintItem> Items { get; }
        }
    }
}
