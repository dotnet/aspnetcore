// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Components.Forms;

public class ClientValidationAdapterRegistryTest
{
    [Fact]
    public void GetAdapter_ReturnsNull_WhenNoAdapterRegistered()
    {
        var registry = new ClientValidationAdapterRegistry();

        var adapter = registry.GetAdapter(new RequiredAttribute());

        Assert.Null(adapter);
    }

    [Fact]
    public void AddAdapter_GetAdapter_RoundTrip()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<RequiredAttribute>(_ => new RequiredClientAdapter());

        var adapter = registry.GetAdapter(new RequiredAttribute());

        Assert.NotNull(adapter);
        Assert.IsType<RequiredClientAdapter>(adapter);
    }

    [Fact]
    public void AddAdapter_PassesAttributeToFactory()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<RangeAttribute>(a => new RangeClientAdapter(a));

        var attribute = new RangeAttribute(1, 100);
        var adapter = registry.GetAdapter(attribute);

        Assert.NotNull(adapter);
        Assert.IsType<RangeClientAdapter>(adapter);
    }

    [Fact]
    public void AddAdapter_LastWins_ReplacesEarlierRegistration()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<RequiredAttribute>(_ => new RequiredClientAdapter());
        registry.AddAdapter<RequiredAttribute>(_ => new CustomAdapter());

        var adapter = registry.GetAdapter(new RequiredAttribute());

        Assert.IsType<CustomAdapter>(adapter);
    }

    [Fact]
    public void GetAdapter_ReturnsSelfAdaptingAttribute()
    {
        var registry = new ClientValidationAdapterRegistry();

        var attribute = new SelfAdaptingAttribute();
        var adapter = registry.GetAdapter(attribute);

        Assert.Same(attribute, adapter);
    }

    [Fact]
    public void GetAdapter_SelfAdaptingTakesPrecedenceOverRegistered()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<SelfAdaptingAttribute>(_ => new CustomAdapter());

        var attribute = new SelfAdaptingAttribute();
        var adapter = registry.GetAdapter(attribute);

        // Self-adapting wins over registered factory
        Assert.Same(attribute, adapter);
    }

    [Fact]
    public void GetAdapter_ReturnsNull_ForUnregisteredAttributeType()
    {
        var registry = new ClientValidationAdapterRegistry();
        registry.AddAdapter<RequiredAttribute>(_ => new RequiredClientAdapter());

        var adapter = registry.GetAdapter(new StringLengthAttribute(100));

        Assert.Null(adapter);
    }

    [Fact]
    public void AddAdapter_ThrowsOnNullFactory()
    {
        var registry = new ClientValidationAdapterRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            registry.AddAdapter<RequiredAttribute>(null!));
    }

    // --- Test helpers ---

    private class CustomAdapter : IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context, string errorMessage)
        {
            context.MergeAttribute("data-val-custom", errorMessage);
        }
    }

    private class SelfAdaptingAttribute : ValidationAttribute, IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context, string errorMessage)
        {
            context.MergeAttribute("data-val-self", errorMessage);
        }
    }
}
