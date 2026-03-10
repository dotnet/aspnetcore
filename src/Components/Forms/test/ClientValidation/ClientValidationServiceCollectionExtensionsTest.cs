// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Forms;

public class ClientValidationServiceCollectionExtensionsTest
{
    [Fact]
    public void AddClientSideValidation_RegistersService()
    {
        var services = new ServiceCollection();
        services.AddClientSideValidation();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetService<IClientValidationService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddClientSideValidation_RegistersScopedService()
    {
        var services = new ServiceCollection();
        services.AddClientSideValidation();

        using var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var service1 = scope1.ServiceProvider.GetService<IClientValidationService>();
        var service2 = scope2.ServiceProvider.GetService<IClientValidationService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddClientSideValidation_DoesNotOverrideExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddScoped<IClientValidationService, StubValidationService>();
        services.AddClientSideValidation();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IClientValidationService>();

        Assert.IsType<StubValidationService>(service);
    }

    [Fact]
    public void AddClientSideValidation_RegistersBuiltInAdapters()
    {
        var services = new ServiceCollection();
        services.AddClientSideValidation();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IClientValidationService>();

        var model = new ModelWithRequired();
        var fieldIdentifier = new FieldIdentifier(model, nameof(ModelWithRequired.Name));
        var attributes = service.GetValidationAttributes(fieldIdentifier);

        Assert.True(attributes.ContainsKey("data-val-required"));
    }

    [Fact]
    public void AddClientValidationAdapter_CustomAdapterIsUsed()
    {
        var services = new ServiceCollection();
        services.AddClientSideValidation();
        services.AddClientValidationAdapter<CustomTestAttribute>(
            _ => new CustomTestAdapter());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IClientValidationService>();

        var model = new ModelWithCustomAttribute();
        var fieldIdentifier = new FieldIdentifier(model, nameof(ModelWithCustomAttribute.Value));
        var attributes = service.GetValidationAttributes(fieldIdentifier);

        Assert.True(attributes.ContainsKey("data-val-custom"));
        Assert.Equal("The field Value is invalid.", attributes["data-val-custom"]);
    }

    [Fact]
    public void AddClientValidationAdapter_CanOverrideBuiltInAdapter()
    {
        var services = new ServiceCollection();
        services.AddClientSideValidation();
        services.AddClientValidationAdapter<RequiredAttribute>(
            _ => new OverridingRequiredAdapter());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IClientValidationService>();

        var model = new ModelWithRequired();
        var fieldIdentifier = new FieldIdentifier(model, nameof(ModelWithRequired.Name));
        var attributes = service.GetValidationAttributes(fieldIdentifier);

        // Custom adapter overrides built-in (last-wins)
        Assert.True(attributes.ContainsKey("data-val-custom-required"));
        Assert.DoesNotContain("data-val-required", attributes.Keys);
    }

    // --- Test helpers ---

    private class StubValidationService : IClientValidationService
    {
        public IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier)
            => new Dictionary<string, string>();
    }

    private class CustomTestAdapter : IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context, string errorMessage)
        {
            context.MergeAttribute("data-val-custom", errorMessage);
        }
    }

    private class OverridingRequiredAdapter : IClientValidationAdapter
    {
        public void AddClientValidation(in ClientValidationContext context, string errorMessage)
        {
            context.MergeAttribute("data-val-custom-required", errorMessage);
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CustomTestAttribute : ValidationAttribute
    {
    }

    public class ModelWithCustomAttribute
    {
        [CustomTest]
        public string Value { get; set; } = "";
    }

    public class ModelWithRequired
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = "";
    }
}
