// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Performance
{
    [Config(typeof(CoreConfig))]
    public class ActionSelectorBenchmark
    {
        private const int Seed = 1000;

        // About 35 or so plausible sounding conventional routing actions.
        //
        // We include some duplicates here, because that's what happens when you have one method that handles
        // GET and one that handles POST.
        private static readonly ActionDescriptor[] _actions = new ActionDescriptor[]
        {
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "AddUser" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "AddUser" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "DeleteUser" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "DeleteUser" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "Details" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Account", action = "List" }),

            CreateActionDescriptor(new { area = "Admin", controller = "Diagnostics", action = "Stats" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Diagnostics", action = "Performance" }),

            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "CreateProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "CreateProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "DeleteProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "DeleteProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "EditProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "EditProduct" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "Index" }),
            CreateActionDescriptor(new { area = "Admin", controller = "Products", action = "Inventory" }),

            CreateActionDescriptor(new { area = "Store", controller = "Search", action = "FindProduct" }),
            CreateActionDescriptor(new { area = "Store", controller = "Search", action = "ShowCategory" }),
            CreateActionDescriptor(new { area = "Store", controller = "Search", action = "HotItems" }),

            CreateActionDescriptor(new { area = "Store", controller = "Product", action = "Index" }),
            CreateActionDescriptor(new { area = "Store", controller = "Product", action = "Details" }),
            CreateActionDescriptor(new { area = "Store", controller = "Product", action = "Buy" }),

            CreateActionDescriptor(new { area = "Store", controller = "Checkout", action = "ViewCart" }),
            CreateActionDescriptor(new { area = "Store", controller = "Checkout", action = "Billing" }),
            CreateActionDescriptor(new { area = "Store", controller = "Checkout", action = "Confim" }),
            CreateActionDescriptor(new { area = "Store", controller = "Checkout", action = "Confim" }),

            CreateActionDescriptor(new { area = "", controller = "Blog", action = "Index" }),
            CreateActionDescriptor(new { area = "", controller = "Blog", action = "Search" }),
            CreateActionDescriptor(new { area = "", controller = "Blog", action = "ViewPost" }),
            CreateActionDescriptor(new { area = "", controller = "Blog", action = "PostComment" }),

            CreateActionDescriptor(new { area = "", controller = "Home", action = "Index" }),
            CreateActionDescriptor(new { area = "", controller = "Home", action = "Search" }),
            CreateActionDescriptor(new { area = "", controller = "Home", action = "About" }),
            CreateActionDescriptor(new { area = "", controller = "Home", action = "Contact" }),
            CreateActionDescriptor(new { area = "", controller = "Home", action = "Support" }),
        };

        private static readonly KeyValuePair<RouteValueDictionary, IReadOnlyList<ActionDescriptor>>[] _dataSet = GetDataSet(_actions);

        private static readonly IActionSelector _actionSelector = CreateActionSelector(_actions);

        [Benchmark(Description = "conventional action selection implementation")]
        public void SelectCandidates_MatchRouteData()
        {
            var routeContext = new RouteContext(new DefaultHttpContext());

            for (var i = 0; i < _dataSet.Length; i++)
            {
                var routeValues = _dataSet[i].Key;
                var expected = _dataSet[i].Value;

                var state = routeContext.RouteData.PushState(MockRouter.Instance, routeValues, null);

                var actual = _actionSelector.SelectCandidates(routeContext);
                Verify(expected, actual);

                state.Restore();
            }
        }

        [Benchmark(Baseline = true, Description = "conventional action selection baseline")]
        public void SelectCandidates_Baseline()
        {
            var routeContext = new RouteContext(new DefaultHttpContext());

            for (var i = 0; i < _dataSet.Length; i++)
            {
                var routeValues = _dataSet[i].Key;
                var expected = _dataSet[i].Value;

                var state = routeContext.RouteData.PushState(MockRouter.Instance, routeValues, null);

                var actual = NaiveSelectCandiates(_actions, routeContext.RouteData.Values);
                Verify(expected, actual);

                state.Restore();
            }
        }

        // A naive implementation we can use to generate match data for inputs, and for a baseline.
        private static IReadOnlyList<ActionDescriptor> NaiveSelectCandiates(ActionDescriptor[] actions, RouteValueDictionary routeValues)
        {
            var results = new List<ActionDescriptor>();
            for (var i = 0; i < actions.Length; i++)
            {
                var action = actions[i];

                var isMatch = true;
                foreach (var kvp in action.RouteValues)
                {
                    var routeValue = Convert.ToString(routeValues[kvp.Key]) ?? string.Empty;

                    if (string.IsNullOrEmpty(kvp.Value) && string.IsNullOrEmpty(routeValue))
                    {
                        // Match
                    }
                    else if (string.Equals(kvp.Value, routeValue, StringComparison.OrdinalIgnoreCase))
                    {
                        // Match;
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    results.Add(action);
                }
            }

            return results;
        }

        private static ActionDescriptor CreateActionDescriptor(object obj)
        {
            // Our real ActionDescriptors don't use RVD, they use a regular old dictionary.
            // Just using RVD here to understand the anonymous object for brevity.
            var routeValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in new RouteValueDictionary(obj))
            {
                routeValues.Add(kvp.Key, Convert.ToString(kvp.Value) ?? string.Empty);
            }

            return new ActionDescriptor()
            {
                RouteValues = routeValues,
            };
        }

        private static KeyValuePair<RouteValueDictionary, IReadOnlyList<ActionDescriptor>>[] GetDataSet(ActionDescriptor[] actions)
        {
            var random = new Random(Seed);

            var data = new List<KeyValuePair<RouteValueDictionary, IReadOnlyList<ActionDescriptor>>>();

            for (var i = 0; i < actions.Length; i += 2)
            {
                var action = actions[i];
                var routeValues = new RouteValueDictionary(action.RouteValues);
                var matches = NaiveSelectCandiates(actions, routeValues);
                if (matches.Count == 0)
                {
                    throw new InvalidOperationException("This should have at least one match.");
                }


                data.Add(new KeyValuePair<RouteValueDictionary, IReadOnlyList<ActionDescriptor>>(routeValues, matches));
            }

            for (var i = 1; i < actions.Length; i += 3)
            {
                var action = actions[i];
                var routeValues = new RouteValueDictionary(action.RouteValues);

                // Make one of the route values not match.
                routeValues[routeValues.First().Key] = ((string)routeValues.First().Value) + "fkdkfdkkf";

                var matches = NaiveSelectCandiates(actions, routeValues);
                if (matches.Count != 0)
                {
                    throw new InvalidOperationException("This should have 0 matches.");
                }

                data.Add(new KeyValuePair<RouteValueDictionary, IReadOnlyList<ActionDescriptor>>(routeValues, matches));
            }

            return data.ToArray();
        }

        private static void Verify(IReadOnlyList<ActionDescriptor> expected, IReadOnlyList<ActionDescriptor> actual)
        {
            if (expected.Count == 0 && actual == null)
            {
                return;
            }

            if (expected.Count != actual.Count)
            {
                throw new InvalidOperationException("The count is different.");
            }

            for (var i = 0; i < actual.Count; i++)
            {
                if (!object.ReferenceEquals(expected[i], actual[i]))
                {
                    throw new InvalidOperationException("The actions don't match.");
                }
            }
        }

        private static IActionSelector CreateActionSelector(ActionDescriptor[] actions)
        {
            var actionCollection = new MockActionDescriptorCollectionProvider(actions);

            return new ActionSelector(
                actionCollection,
                new ActionConstraintCache(actionCollection, Enumerable.Empty<IActionConstraintProvider>()),
                NullLoggerFactory.Instance);
        }

        private class MockActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
        {
            public MockActionDescriptorCollectionProvider(ActionDescriptor[] actions)
            {
                ActionDescriptors = new ActionDescriptorCollection(actions, 0);
            }

            public ActionDescriptorCollection ActionDescriptors { get; }
        }

        private class MockRouter : IRouter
        {
            public static readonly IRouter Instance = new MockRouter();

            public VirtualPathData GetVirtualPath(VirtualPathContext context)
            {
                throw new NotImplementedException();
            }

            public Task RouteAsync(RouteContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
