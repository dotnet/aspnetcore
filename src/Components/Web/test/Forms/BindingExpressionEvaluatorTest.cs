// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components.Web;

public class BindingExpressionEvaluatorTest
{
    [Fact]
    public void CreateFieldIdentifier_ResolvesASingleHop()
    {
        var model = new LoginModel();

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Email, model);

        Assert.Same(model, identifier.Model);
        Assert.Equal(nameof(LoginModel.Email), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesANestedChain()
    {
        var model = new LoginModel { Address = new Address() };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Address.Street, model);

        Assert.Same(model.Address, identifier.Model);
        Assert.Equal(nameof(Address.Street), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesAnIndexer()
    {
        var model = new LoginModel { Orders = [new Order(), new Order()] };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Orders[1].Total, model);

        Assert.Same(model.Orders[1], identifier.Model);
        Assert.Equal(nameof(Order.Total), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesAnArrayIndex()
    {
        var model = new LoginModel { Tags = ["a", "b"] };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Tags[0], model);

        Assert.Same(model.Tags, identifier.Model);
        Assert.Equal("0", identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_AnchorsAtTheModelWhenTheChainCannotBeEvaluated()
    {
        var anchored = new LoginModel();
        var chain = BuildUnevaluatableChain();

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(chain, anchored);

        Assert.Same(anchored, identifier.Model);
        Assert.Equal(nameof(LoginModel.Email), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_MatchesFieldIdentifierCreateForTheSameExpression()
    {
        var model = new LoginModel { Address = new Address() };
        Expression<Func<string?>> accessor = () => model.Address.Street;

        var walked = BindingExpressionEvaluator.CreateFieldIdentifier(accessor, model);
        var compiled = FieldIdentifier.Create(accessor);

        Assert.Equal(compiled, walked);
    }

    [Fact]
    public void CreateFieldIdentifier_ThrowsWhenAHopEvaluatesToNull()
    {
        var model = new LoginModel();

        Assert.Throws<ArgumentException>(
            () => BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Address!.Street, model));
    }

    private static Expression<Func<string?>> BuildUnevaluatableChain()
    {
        var parameter = Expression.Parameter(typeof(LoginModel), "m");
        var body = Expression.Property(parameter, nameof(LoginModel.Email));
        return Expression.Lambda<Func<string?>>(body);
    }

    private sealed class LoginModel
    {
        public string? Email { get; set; }

        public Address? Address { get; set; }

        public List<Order> Orders { get; set; } = [];

        public string[] Tags { get; set; } = [];
    }

    private sealed class Address
    {
        public string? Street { get; set; }
    }

    private sealed class Order
    {
        public decimal Total { get; set; }
    }
}
