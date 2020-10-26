// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    // The ActionSelectionTable has different code paths for ActionDescriptor and
    // RouteEndpoint for creating a table. We're trying to test both code paths 
    // for creation, but selection works the same for both cases.
    public class ActionSelectionTableTest
    {
        [Fact]
        public void Select_SingleMatch()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Collection(matches, (a) => Assert.Same(actions[0], a));
        }

        [Fact]
        [ReplaceCulture("de-CH", "de-CH")]
        public void Select_ActionDescriptor_SingleMatch_UsesInvariantCulture()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" },
                        { "date", "10/31/2018 07:37:38 -07:00" },
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });
            values.Add(
                "date",
                new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7)));

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Collection(matches, (a) => Assert.Same(actions[0], a));
        }

        [Fact]
        [ReplaceCulture("de-CH", "de-CH")]
        public void Select_Endpoint_SingleMatch_UsesInvariantCulture()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" },
                        { "date", "10/31/2018 07:37:38 -07:00" },
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var table = CreateTableWithEndpoints(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });
            values.Add(
                "date",
                new DateTimeOffset(2018, 10, 31, 7, 37, 38, TimeSpan.FromHours(-7)));

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Collection(matches, (e) => Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>()));
        }

        [Fact]
        public void Select_ActionDescriptor_MultipleMatches()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
            };

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Equal(actions.ToArray(), matches.ToArray());
        }

        [Fact]
        public void Select_Endpoint_MultipleMatches()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
            };

            var table = CreateTableWithEndpoints(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Equal(actions.ToArray(), matches.Select(e => e.Metadata.GetMetadata<ActionDescriptor>()).ToArray());
        }

        [Fact]
        public void Select_NoMatch()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Foo", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void Select_ActionDescriptors_NoMatch_ExcludesAttributeRoutedActions()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/Home",
                    }
                },
            };

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void Select_Endpoint_Match_IncludesAttributeRoutedActions()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/Home",
                    }
                },
            };

            var table = CreateTableWithEndpoints(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Single(matches);
        }

        // In this context `CaseSensitiveMatch` means that the input route values exactly match one of the action
        // descriptor's route values in terms of casing. This is important because we optimize for this case
        // in the implementation.
        [Fact]
        public void Select_Match_CaseSensitiveMatch_IncludesAllCaseInsensitiveMatches()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor() // This won't match the request
                {
                    DisplayName = "A3",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var expected = actions.Take(2).ToArray();

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Equal(expected, matches);
        }

        // In this context `CaseInsensitiveMatch` means that the input route values do not match any action
        // descriptor's route values in terms of casing. This is important because we optimize for the case
        // where the casing matches - the non-matching-casing path is handled a bit differently.
        [Fact]
        public void Select_Match_CaseInsensitiveMatch_IncludesAllCaseInsensitiveMatches()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "home" },
                        { "action", "Index" }
                    },
                },
                new ActionDescriptor() // This won't match the request
                {
                    DisplayName = "A3",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "controller", "Home" },
                        { "action", "About" }
                    },
                },
            };

            var expected = actions.Take(2).ToArray();

            var table = CreateTableWithActionDescriptors(actions);
            var values = new RouteValueDictionary(new { controller = "HOME", action = "iNDex", });

            // Act
            var matches = table.Select(values);

            // Assert
            Assert.Equal(expected, matches);
        }

        [Fact]
        public void Select_Match_CaseSensitiveMatch_MatchesOnEmptyString()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "area", null },
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                }
            };

            var table = CreateTableWithActionDescriptors(actions);

            // Example: In conventional route, one could set non-inline defaults
            // new { area = "", controller = "Home", action = "Index" }
            var values = new RouteValueDictionary(new { area = "", controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            var action = Assert.Single(matches);
            Assert.Same(actions[0], action);
        }

        [Fact]
        public void Select_Match_CaseInsensitiveMatch_MatchesOnEmptyString()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "area", null },
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                }
            };

            var table = CreateTableWithActionDescriptors(actions);

            // Example: In conventional route, one could set non-inline defaults
            // new { area = "", controller = "Home", action = "Index" }
            var values = new RouteValueDictionary(new { area = "", controller = "HoMe", action = "InDeX", });

            // Act
            var matches = table.Select(values);

            // Assert
            var action = Assert.Single(matches);
            Assert.Same(actions[0], action);
        }

        [Fact]
        public void Select_Match_MatchesOnNull()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "area", null },
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                }
            };

            var table = CreateTableWithActionDescriptors(actions);

            // Example: In conventional route, one could set non-inline defaults
            // new { area = (string)null, controller = "Foo", action = "Index" }
            var values = new RouteValueDictionary(new { area = (string)null, controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            var action = Assert.Single(matches);
            Assert.Same(actions[0], action);
        }

        [Fact]
        public void Select_Match_ActionDescriptorWithEmptyRouteValues_MatchesOnEmptyString()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "foo", "" },
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                }
            };

            var table = CreateTableWithActionDescriptors(actions);

            var values = new RouteValueDictionary(new { foo = "", controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            var action = Assert.Single(matches);
            Assert.Same(actions[0], action);
        }

        [Fact]
        public void Select_Match_ActionDescriptorWithEmptyRouteValues_MatchesOnNull()
        {
            var actions = new ActionDescriptor[]
            {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "foo", "" },
                        { "controller", "Home" },
                        { "action", "Index" }
                    },
                }
            };

            var table = CreateTableWithActionDescriptors(actions);

            var values = new RouteValueDictionary(new { foo = (string)null, controller = "Home", action = "Index", });

            // Act
            var matches = table.Select(values);

            // Assert
            var action = Assert.Single(matches);
            Assert.Same(actions[0], action);
        }

        private static ActionSelectionTable<ActionDescriptor> CreateTableWithActionDescriptors(IReadOnlyList<ActionDescriptor> actions)
        {
            return ActionSelectionTable<ActionDescriptor>.Create(new ActionDescriptorCollection(actions, 0));
        }

        private static ActionSelectionTable<Endpoint> CreateTableWithEndpoints(IReadOnlyList<ActionDescriptor> actions)
        {
            var endpoints = actions.Select(a =>
            {
                var metadata = new List<object>(a.EndpointMetadata ?? Array.Empty<object>());
                metadata.Add(a);
                return new Endpoint(
                    requestDelegate: context => Task.CompletedTask, 
                    metadata: new EndpointMetadataCollection(metadata),
                    displayName: a.DisplayName);
            });

            return ActionSelectionTable<ActionDescriptor>.Create(endpoints);
        }
    }
}
