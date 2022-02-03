// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class HasValidatorsValidationMetadataProviderIntegrationTest
{
    [Fact]
    public void HasValidatorsValidationMetadataProvider_IsRegisteredAfterOtherMetadataProviders()
    {
        // HasValidatorsValidationMetadataProvider uses values populated by other details providers to query validator providers
        // This test ensures all other detail providers have had an opportunity to modify validation metadata first.
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddMvc();
        var services = serviceCollection.BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptions<MvcOptions>>();

        Assert.IsType<HasValidatorsValidationMetadataProvider>(options.Value.ModelMetadataDetailsProviders.Last());
    }

    [Fact]
    public void HasValidatorsValidationMetadataProvider_IsRegisteredAfterUserSpecifiedMetadataProvider()
    {
        // Arrange
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging();
        serviceCollection.AddMvc(mvcOptions =>
        {
            mvcOptions.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IQueryable)));
        });
        var services = serviceCollection.BuildServiceProvider();

        // Act
        var options = services.GetRequiredService<IOptions<MvcOptions>>();

        Assert.IsType<HasValidatorsValidationMetadataProvider>(options.Value.ModelMetadataDetailsProviders.Last());
    }
}
