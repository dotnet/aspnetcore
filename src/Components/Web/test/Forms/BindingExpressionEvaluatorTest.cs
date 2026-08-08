// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;

#nullable enable annotations

namespace Microsoft.AspNetCore.Components.Web;

public class BindingExpressionEvaluatorTest
{
    [Fact]
    public void CreateFieldIdentifier_ResolvesASingleHopWithoutADescriptor()
    {
        var model = new LoginModel();

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Email, model, null);

        Assert.Same(model, identifier.Model);
        Assert.Equal(nameof(LoginModel.Email), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesANestedChainThroughDescriptors()
    {
        var model = new LoginModel { Address = new Address() };
        var resolver = new StubResolver(LoginModelDescriptor);

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Address.Street, model, resolver);

        Assert.Same(model.Address, identifier.Model);
        Assert.Equal(nameof(Address.Street), identifier.FieldName);
        Assert.Contains(typeof(LoginModel), resolver.LookedUp);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesANestedChainWithoutDescriptors()
    {
        var model = new LoginModel { Address = new Address() };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Address.Street, model, null);

        Assert.Same(model.Address, identifier.Model);
        Assert.Equal(nameof(Address.Street), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesAnIndexerThroughADescriptor()
    {
        var model = new LoginModel { Orders = [new Order(), new Order()] };
        var resolver = new StubResolver(LoginModelDescriptor, OrderListDescriptor);

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Orders[1].Total, model, resolver);

        Assert.Same(model.Orders[1], identifier.Model);
        Assert.Equal(nameof(Order.Total), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesAnIndexerWithoutDescriptors()
    {
        var model = new LoginModel { Orders = [new Order(), new Order()] };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Orders[1].Total, model, null);

        Assert.Same(model.Orders[1], identifier.Model);
        Assert.Equal(nameof(Order.Total), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_ResolvesAnArrayIndex()
    {
        var model = new LoginModel { Tags = ["a", "b"] };

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Tags[0], model, null);

        Assert.Same(model.Tags, identifier.Model);
        Assert.Equal("0", identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_AnchorsAtTheModelWhenTheChainCannotBeEvaluated()
    {
        var anchored = new LoginModel();
        var chain = BuildUnevaluatableChain();

        var identifier = BindingExpressionEvaluator.CreateFieldIdentifier(chain, anchored, null);

        Assert.Same(anchored, identifier.Model);
        Assert.Equal(nameof(LoginModel.Email), identifier.FieldName);
    }

    [Fact]
    public void CreateFieldIdentifier_MatchesFieldIdentifierCreateForTheSameExpression()
    {
        var model = new LoginModel { Address = new Address() };
        Expression<Func<string?>> accessor = () => model.Address.Street;

        var walked = BindingExpressionEvaluator.CreateFieldIdentifier(accessor, model, null);
        var compiled = FieldIdentifier.Create(accessor);

        Assert.Equal(compiled, walked);
    }

    [Fact]
    public void CreateFieldIdentifier_ThrowsWhenAHopEvaluatesToNull()
    {
        var model = new LoginModel();

        Assert.Throws<ArgumentException>(
            () => BindingExpressionEvaluator.CreateFieldIdentifier(() => model.Address!.Street, model, null));
    }

    // A parameter-rooted expression has no constant to evaluate, so the walk has to anchor.
    private static Expression<Func<string?>> BuildUnevaluatableChain()
    {
        var parameter = Expression.Parameter(typeof(LoginModel), "m");
        var body = Expression.Property(parameter, nameof(LoginModel.Email));
        return Expression.Lambda<Func<string?>>(body);
    }

    private static BindableTypeDescriptor LoginModelDescriptor { get; } = new()
    {
        Type = typeof(LoginModel),
        Members =
        [
            new BindableMemberDescriptor
            {
                Name = nameof(LoginModel.Address),
                MemberType = typeof(Address),
                GetValue = static target => ((LoginModel)target).Address,
            },
            new BindableMemberDescriptor
            {
                Name = nameof(LoginModel.Orders),
                MemberType = typeof(List<Order>),
                GetValue = static target => ((LoginModel)target).Orders,
            },
        ],
    };

    private static BindableTypeDescriptor OrderListDescriptor { get; } = new()
    {
        Type = typeof(List<Order>),
        Indexers =
        [
            new BindableIndexerDescriptor
            {
                IndexType = typeof(int),
                ValueType = typeof(Order),
                GetValue = static (target, index) => ((List<Order>)target)[(int)index!],
            },
        ],
    };

    private sealed class StubResolver(params BindableTypeDescriptor[] descriptors) : IBindableTypeResolver
    {
        private readonly Dictionary<Type, BindableTypeDescriptor> _descriptors = descriptors.ToDictionary(d => d.Type);

        public List<Type> LookedUp { get; } = [];

        public bool TryGetBindableTypeDescriptor(Type type, [NotNullWhen(true)] out BindableTypeDescriptor descriptor)
        {
            LookedUp.Add(type);
            return _descriptors.TryGetValue(type, out descriptor!);
        }
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
