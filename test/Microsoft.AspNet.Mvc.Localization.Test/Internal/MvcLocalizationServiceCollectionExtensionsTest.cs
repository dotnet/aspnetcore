// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Localization.Internal
{
    public class MvcLocalizationServicesTest
    {
        [Fact]
        public void AddLocalizationServices_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            MvcLocalizationServices.AddLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix,
                setupAction: null);

            // Assert
            Assert.Collection(collection,
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IViewLocalizer), service.ServiceType);
                    Assert.Equal(typeof(ViewLocalizer), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(HtmlEncoder), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(StringLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IOptions<>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                });
        }

        [Fact]
        public void AddCustomLocalizers_BeforeAddLocalizationServices_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();
            var testEncoder = new HtmlTestEncoder();

            // Act
            collection.Add(ServiceDescriptor.Singleton(typeof(IHtmlLocalizerFactory), typeof(TestHtmlLocalizerFactory)));
            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(TestHtmlLocalizer<>)));
            collection.Add(ServiceDescriptor.Transient(typeof(IViewLocalizer), typeof(TestViewLocalizer)));
            collection.Add(ServiceDescriptor.Instance(typeof(HtmlEncoder), testEncoder));

            MvcLocalizationServices.AddLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix,
                setupAction: null);

            // Assert
            Assert.Collection(collection,
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(TestHtmlLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(TestHtmlLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IViewLocalizer), service.ServiceType);
                    Assert.Equal(typeof(TestViewLocalizer), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(HtmlEncoder), service.ServiceType);
                    Assert.Same(testEncoder, service.ImplementationInstance);
                },
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(StringLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IOptions<>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                });
        }

        [Fact]
        public void AddCustomLocalizers_AfterAddLocalizationServices_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            collection.Configure<RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new CustomPartialDirectoryViewLocationExpander());
            });

            // Act
            MvcLocalizationServices.AddLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix,
                setupAction: null);

            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(TestHtmlLocalizer<>)));
            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer), typeof(TestViewLocalizer)));
            collection.Add(ServiceDescriptor.Instance(typeof(HtmlEncoder), typeof(HtmlTestEncoder)));

            // Assert
            Assert.Collection(collection,
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IViewLocalizer), service.ServiceType);
                    Assert.Equal(typeof(ViewLocalizer), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(HtmlEncoder), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(StringLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IOptions<>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(TestHtmlLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer), service.ServiceType);
                    Assert.Equal(typeof(TestViewLocalizer), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(HtmlEncoder), service.ServiceType);
                    Assert.Equal(typeof(HtmlTestEncoder), service.ImplementationInstance);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                });
        }

        [Fact]
        public void AddLocalizationServicesWithLocalizationOptions_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            MvcLocalizationServices.AddLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix,
                options => options.ResourcesPath = "Resources");

            // Assert
            Assert.Collection(collection,
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IHtmlLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(HtmlLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IViewLocalizer), service.ServiceType);
                    Assert.Equal(typeof(ViewLocalizer), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(HtmlEncoder), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizerFactory), service.ServiceType);
                    Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IStringLocalizer<>), service.ServiceType);
                    Assert.Equal(typeof(StringLocalizer<>), service.ImplementationType);
                    Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IConfigureOptions<LocalizationOptions>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                },
                service =>
                {
                    Assert.Equal(typeof(IOptions<>), service.ServiceType);
                    Assert.Equal(ServiceLifetime.Singleton, service.Lifetime);
                });
        }
    }

    public class TestViewLocalizer : IViewLocalizer
    {
        public LocalizedString this[string name]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures)
        {
            throw new NotImplementedException();
        }

        public LocalizedHtmlString Html(string key)
        {
            throw new NotImplementedException();
        }

        public LocalizedHtmlString Html(string key, params object[] arguments)
        {
            throw new NotImplementedException();
        }

        public IHtmlLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TestHtmlLocalizer<HomeController> : IHtmlLocalizer<HomeController>
    {
        public LocalizedString this[string name]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeAncestorCultures)
        {
            throw new NotImplementedException();
        }

        public LocalizedHtmlString Html(string key)
        {
            throw new NotImplementedException();
        }

        public LocalizedHtmlString Html(string key, params object[] arguments)
        {
            throw new NotImplementedException();
        }

        public IHtmlLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        IStringLocalizer IStringLocalizer.WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TestHtmlLocalizerFactory : IHtmlLocalizerFactory
    {
        public IHtmlLocalizer Create(Type resourceSource)
        {
            throw new NotImplementedException();
        }

        public IHtmlLocalizer Create(string baseName, string location)
        {
            throw new NotImplementedException();
        }
    }

    public class CustomPartialDirectoryViewLocationExpander : IViewLocationExpander
    {
        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            throw new NotImplementedException();
        }

        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }
    }
}
