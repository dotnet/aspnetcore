// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.DecisionTree;

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
        item.Criteria.Add("area", new DecisionCriterionValue(value: "Admin"));
        item.Criteria.Add("controller", new DecisionCriterionValue(value: "Users"));
        item.Criteria.Add("action", new DecisionCriterionValue(value: "AddUser"));
        items.Add(item);

        // Act
        var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

        // Assert
        Assert.Empty(tree.Matches);

        var area = Assert.Single(tree.Criteria);
        Assert.Equal("area", area.Key);

        var admin = Assert.Single(area.Branches);
        Assert.Equal("Admin", admin.Key);
        Assert.Empty(admin.Value.Matches);

        var controller = Assert.Single(admin.Value.Criteria);
        Assert.Equal("controller", controller.Key);

        var users = Assert.Single(controller.Branches);
        Assert.Equal("Users", users.Key);
        Assert.Empty(users.Value.Matches);

        var action = Assert.Single(users.Value.Criteria);
        Assert.Equal("action", action.Key);

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
        item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy"));
        items.Add(item1);

        var item2 = new Item();
        item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout"));
        items.Add(item2);

        // Act
        var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

        // Assert
        Assert.Empty(tree.Matches);

        var action = Assert.Single(tree.Criteria);
        Assert.Equal("action", action.Key);

        var buy = action.Branches["Buy"];
        Assert.Empty(buy.Matches);

        var controller = Assert.Single(buy.Criteria);
        Assert.Equal("controller", controller.Key);

        var store = Assert.Single(controller.Branches);
        Assert.Equal("Store", store.Key);
        Assert.Empty(store.Value.Criteria);
        Assert.Same(item1, Assert.Single(store.Value.Matches));

        var checkout = action.Branches["Checkout"];
        Assert.Empty(checkout.Matches);

        controller = Assert.Single(checkout.Criteria);
        Assert.Equal("controller", controller.Key);

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
        item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy"));
        items.Add(item1);

        var item2 = new Item();
        item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout"));
        items.Add(item2);

        var item3 = new Item();
        item3.Criteria.Add("action", new DecisionCriterionValue(value: "Buy"));
        items.Add(item3);

        // Act
        var tree = DecisionTreeBuilder<Item>.GenerateTree(items, new ItemClassifier());

        // Assert
        Assert.Empty(tree.Matches);

        var action = Assert.Single(tree.Criteria);
        Assert.Equal("action", action.Key);

        var buy = action.Branches["Buy"];
        Assert.Same(item3, Assert.Single(buy.Matches));
    }

    [Fact]
    public void BuildTree_WithDivergentCriteria()
    {
        // Arrange
        var items = new List<Item>();

        var item1 = new Item();
        item1.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item1.Criteria.Add("action", new DecisionCriterionValue(value: "Buy"));
        items.Add(item1);

        var item2 = new Item();
        item2.Criteria.Add("controller", new DecisionCriterionValue(value: "Store"));
        item2.Criteria.Add("action", new DecisionCriterionValue(value: "Checkout"));
        items.Add(item2);

        var item3 = new Item();
        item3.Criteria.Add("stub", new DecisionCriterionValue(value: "Bleh"));
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
        public IEqualityComparer<object> ValueComparer => RouteValueEqualityComparer.Default;

        public IDictionary<string, DecisionCriterionValue> GetCriteria(Item item)
        {
            return item.Criteria;
        }
    }
}
