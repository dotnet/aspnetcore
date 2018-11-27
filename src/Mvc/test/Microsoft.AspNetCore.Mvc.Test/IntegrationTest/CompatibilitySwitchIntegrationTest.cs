// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTest
{
    // Integration tests for compatibility switches. These tests verify which compatibility
    // values apply to each supported version.
    //
    // If you add a new compatibility switch, make sure to update ALL of these tests. Each test
    // here should include verification for all of the switches.
    public class CompatibilitySwitchIntegrationTest
    {
        [Fact]
        public void CompatibilitySwitches_Version_2_0()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection
                .AddMvc()
                .AddXmlDataContractSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_0);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;
            var jsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value;
            var razorPagesOptions = services.GetRequiredService<IOptions<RazorPagesOptions>>().Value;
            var apiBehaviorOptions = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var razorViewEngineOptions = services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            var xmlOptions = services.GetRequiredService<IOptions<MvcXmlOptions>>().Value;

            // Assert
            Assert.False(mvcOptions.AllowCombiningAuthorizeFilters);
            Assert.False(mvcOptions.AllowBindingHeaderValuesToNonStringModelTypes);
            Assert.False(mvcOptions.SuppressBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionPolicy.AllExceptions, mvcOptions.InputFormatterExceptionPolicy);
            Assert.False(jsonOptions.AllowInputFormatterExceptionMessages);
            Assert.False(razorPagesOptions.AllowAreas);
            Assert.False(mvcOptions.EnableEndpointRouting);
            Assert.Null(mvcOptions.MaxValidationDepth);
            Assert.True(apiBehaviorOptions.SuppressUseValidationProblemDetailsForInvalidModelStateResponses);
            Assert.True(apiBehaviorOptions.SuppressMapClientErrors);
            Assert.False(razorPagesOptions.AllowDefaultHandlingForOptionsRequests);
            Assert.False(xmlOptions.AllowRfc7807CompliantProblemDetailsFormat);
            Assert.False(mvcOptions.AllowShortCircuitingValidationWhenNoValidatorsArePresent);
            Assert.True(apiBehaviorOptions.AllowInferringBindingSourceForCollectionTypesAsFromQuery);
        }

        [Fact]
        public void CompatibilitySwitches_Version_2_1()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection
                .AddMvc()
                .AddXmlDataContractSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;
            var jsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value;
            var razorPagesOptions = services.GetRequiredService<IOptions<RazorPagesOptions>>().Value;
            var apiBehaviorOptions = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var razorViewEngineOptions = services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            var xmlOptions = services.GetRequiredService<IOptions<MvcXmlOptions>>().Value;

            // Assert
            Assert.True(mvcOptions.AllowCombiningAuthorizeFilters);
            Assert.True(mvcOptions.AllowBindingHeaderValuesToNonStringModelTypes);
            Assert.True(mvcOptions.SuppressBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionPolicy.MalformedInputExceptions, mvcOptions.InputFormatterExceptionPolicy);
            Assert.True(jsonOptions.AllowInputFormatterExceptionMessages);
            Assert.True(razorPagesOptions.AllowAreas);
            Assert.False(mvcOptions.EnableEndpointRouting);
            Assert.Null(mvcOptions.MaxValidationDepth);
            Assert.True(apiBehaviorOptions.SuppressUseValidationProblemDetailsForInvalidModelStateResponses);
            Assert.True(apiBehaviorOptions.SuppressMapClientErrors);
            Assert.False(razorPagesOptions.AllowDefaultHandlingForOptionsRequests);
            Assert.False(xmlOptions.AllowRfc7807CompliantProblemDetailsFormat);
            Assert.False(mvcOptions.AllowShortCircuitingValidationWhenNoValidatorsArePresent);
            Assert.True(apiBehaviorOptions.AllowInferringBindingSourceForCollectionTypesAsFromQuery);
        }

        [Fact]
        public void CompatibilitySwitches_Version_2_2()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection
                .AddMvc()
                .AddXmlDataContractSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;
            var jsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value;
            var razorPagesOptions = services.GetRequiredService<IOptions<RazorPagesOptions>>().Value;
            var apiBehaviorOptions = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var razorViewEngineOptions = services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            var xmlOptions = services.GetRequiredService<IOptions<MvcXmlOptions>>().Value;

            // Assert
            Assert.True(mvcOptions.AllowCombiningAuthorizeFilters);
            Assert.True(mvcOptions.AllowBindingHeaderValuesToNonStringModelTypes);
            Assert.True(mvcOptions.SuppressBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionPolicy.MalformedInputExceptions, mvcOptions.InputFormatterExceptionPolicy);
            Assert.True(jsonOptions.AllowInputFormatterExceptionMessages);
            Assert.True(razorPagesOptions.AllowAreas);
            Assert.True(mvcOptions.EnableEndpointRouting);
            Assert.Equal(32, mvcOptions.MaxValidationDepth);
            Assert.False(apiBehaviorOptions.SuppressUseValidationProblemDetailsForInvalidModelStateResponses);
            Assert.False(apiBehaviorOptions.SuppressMapClientErrors);
            Assert.True(razorPagesOptions.AllowDefaultHandlingForOptionsRequests);
            Assert.True(xmlOptions.AllowRfc7807CompliantProblemDetailsFormat);
            Assert.True(mvcOptions.AllowShortCircuitingValidationWhenNoValidatorsArePresent);
            Assert.False(apiBehaviorOptions.AllowInferringBindingSourceForCollectionTypesAsFromQuery);
        }

        [Fact]
        public void CompatibilitySwitches_Version_Latest()
        {
            // Arrange
            var serviceCollection = new ServiceCollection();
            AddHostingServices(serviceCollection);
            serviceCollection
                .AddMvc()
                .AddXmlDataContractSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            var services = serviceCollection.BuildServiceProvider();

            // Act
            var mvcOptions = services.GetRequiredService<IOptions<MvcOptions>>().Value;
            var jsonOptions = services.GetRequiredService<IOptions<MvcJsonOptions>>().Value;
            var razorPagesOptions = services.GetRequiredService<IOptions<RazorPagesOptions>>().Value;
            var apiBehaviorOptions = services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            var razorViewEngineOptions = services.GetRequiredService<IOptions<RazorViewEngineOptions>>().Value;
            var xmlOptions = services.GetRequiredService<IOptions<MvcXmlOptions>>().Value;

            // Assert
            Assert.True(mvcOptions.AllowCombiningAuthorizeFilters);
            Assert.True(mvcOptions.AllowBindingHeaderValuesToNonStringModelTypes);
            Assert.True(mvcOptions.SuppressBindingUndefinedValueToEnumType);
            Assert.Equal(InputFormatterExceptionPolicy.MalformedInputExceptions, mvcOptions.InputFormatterExceptionPolicy);
            Assert.True(jsonOptions.AllowInputFormatterExceptionMessages);
            Assert.True(razorPagesOptions.AllowAreas);
            Assert.True(mvcOptions.EnableEndpointRouting);
            Assert.Equal(32, mvcOptions.MaxValidationDepth);
            Assert.False(apiBehaviorOptions.SuppressUseValidationProblemDetailsForInvalidModelStateResponses);
            Assert.False(apiBehaviorOptions.SuppressMapClientErrors);
            Assert.True(razorPagesOptions.AllowDefaultHandlingForOptionsRequests);
            Assert.True(xmlOptions.AllowRfc7807CompliantProblemDetailsFormat);
            Assert.True(mvcOptions.AllowShortCircuitingValidationWhenNoValidatorsArePresent);
            Assert.False(apiBehaviorOptions.AllowInferringBindingSourceForCollectionTypesAsFromQuery);
        }

        // This just does the minimum needed to be able to resolve these options.
        private static void AddHostingServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(Mock.Of<IHostingEnvironment>());
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        }
    }
}
