// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.Extensions.Validation.Tests;

public class ValidatableInfoResolverTests
{
    public delegate void TryGetValidatableTypeInfoCallback(Type type, out IValidatableTypeInfo? validatableInfo);

    [Fact]
    public void ResolversChain_ProcessesInCorrectOrder()
    {
        var services = new ServiceCollection();
        var resolver1 = new Mock<IValidatableInfoResolver>(MockBehavior.Strict);
        var resolver2 = new Mock<IValidatableInfoResolver>(MockBehavior.Strict);
        var resolver3 = new Mock<IValidatableInfoResolver>(MockBehavior.Strict);
        var typeInfo = new CannedValidatableTypeInfo();

        resolver1
            .Setup(r => r.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out It.Ref<IValidatableTypeInfo?>.IsAny))
            .Callback(new TryGetValidatableTypeInfoCallback((Type _, out IValidatableTypeInfo? info) => info = null))
            .Returns(false);

        resolver2
            .Setup(r => r.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out It.Ref<IValidatableTypeInfo?>.IsAny))
            .Callback(new TryGetValidatableTypeInfoCallback((Type _, out IValidatableTypeInfo? info) => info = typeInfo))
            .Returns(true);

        services.AddValidation(options =>
        {
            options.Resolvers.Add(resolver1.Object);
            options.Resolvers.Add(resolver2.Object);
            options.Resolvers.Add(resolver3.Object);
        });

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ValidationOptions>>().Value;
        var result = options.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out var validatableInfo);

        Assert.True(result);
        Assert.Same(typeInfo, validatableInfo);
        resolver1.Verify(r => r.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out It.Ref<IValidatableTypeInfo?>.IsAny), Times.Once);
        resolver2.Verify(r => r.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out It.Ref<IValidatableTypeInfo?>.IsAny), Times.Once);
        resolver3.Verify(r => r.TryGetValidatableTypeInfo(typeof(ResolverOrderModel), out It.Ref<IValidatableTypeInfo?>.IsAny), Times.Never);
    }

    private sealed class ParameterOnlyResolver : IValidatableInfoResolver
    {
        public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
        }

        public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo? validatableInfo)
        {
            validatableInfo = null;
            return false;
        }
    }
}

[ValidatableType]
public sealed class ResolverOrderModel
{
    public string? Name { get; set; }
}
