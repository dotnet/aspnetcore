// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Localization.Test
{
    public class MvcLocalizationMvcBuilderExtensionsTest
    {
        public static TheoryData<IMvcBuilder> MvcBuilderExtensionsData()
        {
            var builder1 = new TestMvcBuilder();
            builder1.AddMvcLocalization();

            var builder2 = new TestMvcBuilder();
            builder2.AddMvcLocalization(LanguageViewLocationExpanderFormat.SubFolder);

            var builder3 = new TestMvcBuilder();
            builder3.AddMvcLocalization(localizationOptionsSetupAction: l => l.ResourcesPath = "Resources");

            var builder4 = new TestMvcBuilder();
            builder4.AddMvcLocalization(
                localizationOptionsSetupAction: l => l.ResourcesPath = "Resources",
                format: LanguageViewLocationExpanderFormat.SubFolder);

            return new TheoryData<IMvcBuilder>()
            {
                builder1, builder2, builder3, builder4
            };
        }

        [Theory]
        [MemberData(nameof(MvcBuilderExtensionsData))]
        public void AddsRequiredServices(IMvcBuilder mvcBuilder)
        {
            // Assert
            var services = mvcBuilder.Services;
            // Base localization services
            var service = services.FirstOrDefault(
                sd => sd.ServiceType == typeof(IStringLocalizerFactory));
            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
            Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), service.ImplementationType);

            service = services.FirstOrDefault(
                sd => sd.ServiceType == typeof(IStringLocalizer<>));
            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(StringLocalizer<>), service.ImplementationType);

            // View localization services
            service = services.FirstOrDefault(
                sd => sd.ServiceType == typeof(IConfigureOptions<MvcDataAnnotationsLocalizationOptions>));
            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);

            service = services.FirstOrDefault(
                sd => sd.ServiceType == typeof(IConfigureOptions<RazorViewEngineOptions>));
            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);

            service = services.FirstOrDefault(sd => sd.ServiceType == typeof(IHtmlLocalizerFactory));
            Assert.NotNull(service);
            Assert.Equal(typeof(HtmlLocalizerFactory), service.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);

            service = services.FirstOrDefault(sd => sd.ServiceType == typeof(IHtmlLocalizer<>));
            Assert.NotNull(service);
            Assert.Equal(typeof(HtmlLocalizer<>), service.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);

            service = services.FirstOrDefault(sd => sd.ServiceType == typeof(IViewLocalizer));
            Assert.NotNull(service);
            Assert.Equal(typeof(ViewLocalizer), service.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
        }

        [Fact]
        public void SetsLocalizationOptions_AsExpected()
        {
            // Arrange
            var builder = new TestMvcBuilder();

            // Act
            builder.AddMvcLocalization(
                localizationOptionsSetupAction: options => options.ResourcesPath = "TestResources");

            // Assert
            var serviceProvider = builder.Services.BuildServiceProvider();
            var actualOptions = serviceProvider.GetRequiredService<IOptions<LocalizationOptions>>();
            Assert.Equal("TestResources", actualOptions.Value.ResourcesPath);
        }

        [Fact]
        public void SetsDataAnnotationsOptions_AsExpected()
        {
            // Arrange
            var builder = new TestMvcBuilder();
            var dataAnnotationLocalizerProvider = new Func<Type, IStringLocalizerFactory, IStringLocalizer>((type, factory) =>
            {
                return null;
            });

            // Act
            builder.AddMvcLocalization(
                dataAnnotationsLocalizationOptionsSetupAction: options
                => options.DataAnnotationLocalizerProvider = dataAnnotationLocalizerProvider);

            // Assert
            var serviceProvider = builder.Services.BuildServiceProvider();
            var actualOptions = serviceProvider.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
            Assert.Same(dataAnnotationLocalizerProvider, actualOptions.Value.DataAnnotationLocalizerProvider);
        }

        private ServiceDescriptor GetService(IServiceCollection services, Type serviceType)
        {
            return services.FirstOrDefault(sd => sd.ServiceType == serviceType);
        }

        private class TestMvcBuilder : IMvcBuilder
        {
            IServiceCollection _services;
            public IServiceCollection Services
            {
                get
                {
                    if (_services == null)
                    {
                        _services = new ServiceCollection();
                    }
                    return _services;
                }
            }

            public ApplicationPartManager PartManager => new ApplicationPartManager();
        }
    }
}
