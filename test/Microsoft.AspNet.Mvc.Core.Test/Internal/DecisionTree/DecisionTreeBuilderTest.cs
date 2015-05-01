// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Routing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Internal.DecisionTree
{
    public class DecisionTreeBuilderTest
    {
        [Fact]
        public void BuildTree_Empty()
        {
            // Arrange
            var items = new List<Item>();

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Criteria);
            Assert.Empty(tree.Matches);
        }

        [Fact]
        public void BuildTree_TrivialMatch()
        {
            // Arrange
            var items = new List<Item>();

            var item = new Item();
            items.Add(item);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Criteria);
            Assert.Same(item, Assert.Single(tree.Matches));
        }

        [Fact]
        public void BuildTree_WithMultipleCriteria()
        {
            // Arrange
            var items = new List<Item>();

            var item = new Item();
            item.Criteria.Add("area", new DecisionCriterionValue(value: "Admin", isCatchAll: false));
            item.Criteria.Add("controller", new DecisionCriterionValue(value: "Users", isCatchAll: false));
            item.Criteria.Add("action", new DecisionCriterionValue(value: "AddUser", isCatchAll: false));
            items.Add(item);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Matches);

            var area = Assert.Single(tree.Criteria);
            Assert.Equal("area", area.Key);
            Assert.Null(area.Fallback);

            var admin = Assert.Single(area.Branches);
            Assert.Equal("Admin", admin.Key);
            Assert.Empty(admin.Value.Matches);

            var controller = Assert.Single(admin.Value.Criteria);
            Assert.Equal("controller", controller.Key);
            Assert.Null(controller.Fallback);

            var users = Assert.Single(controller.Branches);
            Assert.Equal("Users", users.Key);
            Assert.Empty(users.Value.Matches);

            var action = Assert.Single(users.Value.Criteria);
            Assert.Equal("action", action.Key);
            Assert.Null(action.Fallback);

            var addUser = Assert.Single(action.Branches);
            Assert.Equal("AddUser", addUser.Key);
            Assert.Empty(addUser.Value.Criteria);
            Assert.Same(item, Assert.Single(addUser.Value.Matches));
        }

        [Fact]
        public void BuildTree_WithMultipleItems()
        {
            // Arrange
            var items = new List<Item>();

            var item1 = new Item();
            item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy", isCatchAll: false));
            items.Add(item1);

            var item2 = new Item();
            item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item2);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Matches);

            var action = Assert.Single(tree.Criteria);
            Assert.Equal("action", action.Key);
            Assert.Null(action.Fallback);

            var buy = action.Branches["Buy"];
            Assert.Empty(buy.Matches);

            var controller = Assert.Single(buy.Criteria);
            Assert.Equal("controller", controller.Key);
            Assert.Null(controller.Fallback);

            var store = Assert.Single(controller.Branches);
            Assert.Equal("Store", store.Key);
            Assert.Empty(store.Value.Criteria);
            Assert.Same(item1, Assert.Single(store.Value.Matches));

            var checkout = action.Branches["Checkout"];
            Assert.Empty(checkout.Matches);

            controller = Assert.Single(checkout.Criteria);
            Assert.Equal("controller", controller.Key);
            Assert.Null(controller.Fallback);

            store = Assert.Single(controller.Branches);
            Assert.Equal("Store", store.Key);
            Assert.Empty(store.Value.Criteria);
            Assert.Same(item2, Assert.Single(store.Value.Matches));
        }

        [Fact]
        public void BuildTree_WithInteriorMatch()
        {
            // Arrange
            var items = new List<Item>();

            var item1 = new Item();
            item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy", isCatchAll: false));
            items.Add(item1);

            var item2 = new Item();
            item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item2);

            var item3 = new Item();
            item3.Criteria.Add("action", new DecisionCriterionValue(value: "Buy", isCatchAll: false));
            items.Add(item3);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Matches);

            var action = Assert.Single(tree.Criteria);
            Assert.Equal("action", action.Key);
            Assert.Null(action.Fallback);

            var buy = action.Branches["Buy"];
            Assert.Same(item3, Assert.Single(buy.Matches));
        }

        [Fact]
        public void BuildTree_WithCatchAll()
        {
            // Arrange
            var items = new List<Item>();

            var item1 = new Item();
            item1.Criteria.Add("country", new DecisionCriterionValue(value: "CA", isCatchAll: false));
            item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item1.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item1);

            var item2 = new Item();
            item2.Criteria.Add("country", new DecisionCriterionValue(value: "US", isCatchAll: false));
            item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item2);

            var item3 = new Item();
            item3.Criteria.Add("country", new DecisionCriterionValue(value: null, isCatchAll: true));
            item3.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item3.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item3);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Matches);

            var country = Assert.Single(tree.Criteria);
            Assert.Equal("country", country.Key);

            var fallback = country.Fallback;
            Assert.NotNull(fallback);

            var controller = Assert.Single(fallback.Criteria);
            Assert.Equal("controller", controller.Key);
            Assert.Null(controller.Fallback);

            var store = Assert.Single(controller.Branches);
            Assert.Equal("Store", store.Key);
            Assert.Empty(store.Value.Matches);

            var action = Assert.Single(store.Value.Criteria);
            Assert.Equal("action", action.Key);
            Assert.Null(action.Fallback);

            var checkout = Assert.Single(action.Branches);
            Assert.Equal("Checkout", checkout.Key);
            Assert.Empty(checkout.Value.Criteria);
            Assert.Same(item3, Assert.Single(checkout.Value.Matches));
        }

        [Fact]
        public void BuildTree_WithDivergentCriteria()
        {
            // Arrange
            var items = new List<Item>();

            var item1 = new Item();
            item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy", isCatchAll: false));
            items.Add(item1);

            var item2 = new Item();
            item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store", isCatchAll: false));
            item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout", isCatchAll: false));
            items.Add(item2);

            var item3 = new Item();
            item3.Criteria.Add("stub", new DecisionCriterionValue(value: "Bleh", isCatchAll: false));
            items.Add(item3);

            // Act
            var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

            // Assert
            Assert.Empty(tree.Matches);

            var action = tree.Criteria[0];
            Assert.Equal("action", action.Key);

            var stub = tree.Criteria[1];
            Assert.Equal("stub", stub.Key);
        }

        private class Item
        {
            public Item()
            {
                Criteria = new Dictionary<string, DecisionCriterionValue>(StringComparer.OrdinalIgnoreCase);
            }

            public Dictionary<string, DecisionCriterionValue> Criteria { get; private set; }
        }

        private class ItemClassifier : IClassifier<Item>
        {
            public IEqualityComparer<object> ValueComparer
            {
                get
                {
                    return new RouteValueEqualityComparer();
                }
            }

            public IDictionary<string, DecisionCriterionValue> GetCriteria(Item item)
            {
                return item.Criteria;
            }
        }
    }
}