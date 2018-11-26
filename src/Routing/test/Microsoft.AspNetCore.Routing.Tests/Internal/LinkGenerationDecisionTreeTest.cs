// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Internal.Routing
{
    public class LinkGenerationDecisionTreeTest
    {
        [Fact]
        public void GetMatches_AllowsNullAmbientValues()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { });

            // Act
            var matches = tree.GetMatches(context.Values, ambientValues: null);

            // Assert
            Assert.Same(entry, Assert.Single(matches).Match);
        }

        [Fact]
        public void SelectSingleEntry_NoCriteria()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            Assert.Same(entry, Assert.Single(matches).Match);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            Assert.Same(entry, Assert.Single(matches).Match);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria_AmbientValues()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(values: null, ambientValues: new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            var match = Assert.Single(matches);
            Assert.Same(entry, match.Match);
            Assert.False(match.IsFallbackMatch);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria_Replaced()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(
                values: new { action = "Buy" },
                ambientValues: new { controller = "Store", action = "Cart" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            var match = Assert.Single(matches);
            Assert.Same(entry, match.Match);
            Assert.False(match.IsFallbackMatch);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria_AmbientValue_Ignored()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = (string)null });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(
                values: new { controller = "Store" },
                ambientValues: new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            var match = Assert.Single(matches);
            Assert.Same(entry, match.Match);
            Assert.True(match.IsFallbackMatch);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria_NoMatch()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "AddToCart" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void SelectSingleEntry_MultipleCriteria_AmbientValue_NoMatch()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(
                values: new { controller = "Store" },
                ambientValues: new { controller = "Store", action = "Cart" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void SelectMultipleEntries_OneDoesntMatch()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry1);

            var entry2 = CreateMatch(new { controller = "Store", action = "Cart" });
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(
                values: new { controller = "Store" },
                ambientValues: new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues);

            // Assert
            Assert.Same(entry1, Assert.Single(matches).Match);
        }

        [Fact]
        public void SelectMultipleEntries_BothMatch_CriteriaSubset()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry1);

            var entry2 = CreateMatch(new { controller = "Store" });
            entry2.Entry.Order = 1;
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(
                values: new { controller = "Store" },
                ambientValues: new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues).Select(m => m.Match).ToList();

            // Assert
            Assert.Equal(entries, matches);
        }

        [Fact]
        public void SelectMultipleEntries_BothMatch_NonOverlappingCriteria()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entries.Add(entry1);

            var entry2 = CreateMatch(new { slug = "1234" });
            entry2.Entry.Order = 1;
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "Buy", slug = "1234" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues).Select(m => m.Match).ToList();

            // Assert
            Assert.Equal(entries, matches);
        }

        // Precedence is ignored for sorting because they have different order
        [Fact]
        public void SelectMultipleEntries_BothMatch_OrderedByOrder()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry1.Entry.Precedence = 0;
            entries.Add(entry1);

            var entry2 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry2.Entry.Order = 1;
            entry2.Entry.Precedence = 1;
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues).Select(m => m.Match).ToList();

            // Assert
            Assert.Equal(entries, matches);
        }

        // Precedence is used for sorting because they have the same order
        [Fact]
        public void SelectMultipleEntries_BothMatch_OrderedByPrecedence()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry1.Entry.Precedence = 1;
            entries.Add(entry1);

            var entry2 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry2.Entry.Precedence = 0;
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues).Select(m => m.Match).ToList();

            // Assert
            Assert.Equal(entries, matches);
        }

        // Template is used for sorting because they have the same order
        [Fact]
        public void SelectMultipleEntries_BothMatch_OrderedByTemplate()
        {
            // Arrange
            var entries = new List<OutboundMatch>();

            var entry1 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry1.Entry.RouteTemplate = TemplateParser.Parse("a");
            entries.Add(entry1);

            var entry2 = CreateMatch(new { controller = "Store", action = "Buy" });
            entry2.Entry.RouteTemplate = TemplateParser.Parse("b");
            entries.Add(entry2);

            var tree = new LinkGenerationDecisionTree(entries);

            var context = CreateContext(new { controller = "Store", action = "Buy" });

            // Act
            var matches = tree.GetMatches(context.Values, context.AmbientValues).Select(m => m.Match).ToList();

            // Assert
            Assert.Equal(entries, matches);
        }

        [Fact]
        public void ToDebuggerDisplayString_GivesAFlattenedTree()
        {
            // Arrange
            var entries = new List<OutboundMatch>();
            entries.Add(CreateMatch(new { action = "Buy", controller = "Store", version = "V1" }, "Store/Buy/V1"));
            entries.Add(CreateMatch(new { action = "Buy", controller = "Store", area = "Admin" }, "Admin/Store/Buy"));
            entries.Add(CreateMatch(new { action = "Buy", controller = "Products" }, "Products/Buy"));
            entries.Add(CreateMatch(new { action = "Buy", controller = "Store", version = "V2" }, "Store/Buy/V2"));
            entries.Add(CreateMatch(new { action = "Cart", controller = "Store" }, "Store/Cart"));
            entries.Add(CreateMatch(new { action = "Index", controller = "Home" }, "Home/Index/{id?}"));
            var tree = new LinkGenerationDecisionTree(entries);
            var newLine = Environment.NewLine;
            var expected =
                " => action: Buy => controller: Store => version: V1 (Matches: Store/Buy/V1)" + newLine +
                " => action: Buy => controller: Store => version: V2 (Matches: Store/Buy/V2)" + newLine +
                " => action: Buy => controller: Store => area: Admin (Matches: Admin/Store/Buy)" + newLine +
                " => action: Buy => controller: Products (Matches: Products/Buy)" + newLine +
                " => action: Cart => controller: Store (Matches: Store/Cart)" + newLine +
                " => action: Index => controller: Home (Matches: Home/Index/{id?})" + newLine;

            // Act
            var flattenedTree = tree.DebuggerDisplayString;

            // Assert
            Assert.Equal(expected, flattenedTree);
        }

        private OutboundMatch CreateMatch(object requiredValues, string routeTemplate = null)
        {
            var match = new OutboundMatch();
            match.Entry = new OutboundRouteEntry();
            match.Entry.RequiredLinkValues = new RouteValueDictionary(requiredValues);

            if (!string.IsNullOrEmpty(routeTemplate))
            {
                match.Entry.RouteTemplate = new RouteTemplate(RoutePatternFactory.Parse(routeTemplate));
            }

            return match;
        }

        private VirtualPathContext CreateContext(object values, object ambientValues = null)
        {
            var context = new VirtualPathContext(
                new DefaultHttpContext(),
                new RouteValueDictionary(ambientValues),
                new RouteValueDictionary(values));

            return context;
        }
    }
}