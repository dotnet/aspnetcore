// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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

    [Fact]
    public void Select_ManyRouteValues_MatchesAcrossOverLengthRentedBuffer()
    {
        // Select rents its lookup-key buffer from ArrayPool, whose Rent returns an array that is
        // typically longer than requested. This exercises a table with several route keys to ensure
        // the extra trailing slots in the rented buffer never affect hashing or matching.
        var actions = new ActionDescriptor[]
        {
                new ActionDescriptor()
                {
                    DisplayName = "A1",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "area", "Admin" },
                        { "controller", "Home" },
                        { "action", "Index" },
                        { "page", "1" },
                        { "sort", "asc" },
                    },
                },
                new ActionDescriptor()
                {
                    DisplayName = "A2",
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "area", "Admin" },
                        { "controller", "Home" },
                        { "action", "Index" },
                        { "page", "2" },
                        { "sort", "asc" },
                    },
                },
        };

        var table = CreateTableWithActionDescriptors(actions);
        var values = new RouteValueDictionary(new { area = "admin", controller = "home", action = "index", page = "2", sort = "ASC", });

        var matches = table.Select(values);

        Assert.Collection(matches, (a) => Assert.Same(actions[1], a));
    }

    [Fact]
    public void Select_RepeatedCalls_ReturnConsistentResults()
    {
        // The rented buffer is cleared and returned to the pool after every call. Repeated calls must
        // keep returning identical results, proving the buffer reuse does not retain or corrupt state.
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

        for (var i = 0; i < 1000; i++)
        {
            var matches = table.Select(values);
            Assert.Collection(matches, (a) => Assert.Same(actions[0], a));
        }
    }

    [Fact]
    public void Select_ConcurrentCalls_ReturnCorrectResults()
    {
        // Each Select call rents its own buffer, so concurrent lookups with different inputs must not
        // interfere with each other.
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
        var index = new RouteValueDictionary(new { controller = "Home", action = "Index", });
        var about = new RouteValueDictionary(new { controller = "Home", action = "About", });

        Parallel.For(0, 2000, i =>
        {
            if (i % 2 == 0)
            {
                var matches = table.Select(index);
                Assert.Collection(matches, (a) => Assert.Same(actions[0], a));
            }
            else
            {
                var matches = table.Select(about);
                Assert.Collection(matches, (a) => Assert.Same(actions[1], a));
            }
        });
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
