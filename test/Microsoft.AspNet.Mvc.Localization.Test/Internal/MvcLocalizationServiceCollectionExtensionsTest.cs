// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Localization;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;
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
                LanguageViewLocationExpanderFormat.Suffix);

            // Assert
            var services = collection.ToList();
            Assert.Equal(7, services.Count);

            Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), services[0].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[0].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizerFactory), services[1].ServiceType);
            Assert.Equal(typeof(HtmlLocalizerFactory), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[1].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizer<>), services[2].ServiceType);
            Assert.Equal(typeof(HtmlLocalizer<>), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[2].Lifetime);

            Assert.Equal(typeof(IViewLocalizer), services[3].ServiceType);
            Assert.Equal(typeof(ViewLocalizer), services[3].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[3].Lifetime);

            Assert.Equal(typeof(IHtmlEncoder), services[4].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[4].Lifetime);

            Assert.Equal(typeof(IStringLocalizerFactory), services[5].ServiceType);
            Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), services[5].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[5].Lifetime);

            Assert.Equal(typeof(IStringLocalizer<>), services[6].ServiceType);
            Assert.Equal(typeof(StringLocalizer<>), services[6].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[6].Lifetime);
        }

        [Fact]
        public void AddCustomLocalizers_BeforeAddLocalizationServices_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            collection.Add(ServiceDescriptor.Singleton(typeof(IHtmlLocalizerFactory), typeof(TestHtmlLocalizerFactory)));
            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(TestHtmlLocalizer<>)));
            collection.Add(ServiceDescriptor.Transient(typeof(IViewLocalizer), typeof(TestViewLocalizer)));
            collection.Add(ServiceDescriptor.Instance(typeof(IHtmlEncoder), typeof(CommonTestEncoder)));

            MvcLocalizationServices.AddLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix);

            // Assert
            var services = collection.ToList();
            Assert.Equal(7, services.Count);

            Assert.Equal(typeof(IHtmlLocalizerFactory), services[0].ServiceType);
            Assert.Equal(typeof(TestHtmlLocalizerFactory), services[0].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[0].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizer<>), services[1].ServiceType);
            Assert.Equal(typeof(TestHtmlLocalizer<>), services[1].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[1].Lifetime);

            Assert.Equal(typeof(IViewLocalizer), services[2].ServiceType);
            Assert.Equal(typeof(TestViewLocalizer), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[2].Lifetime);

            Assert.Equal(typeof(IHtmlEncoder), services[3].ServiceType);
            Assert.Equal(typeof(CommonTestEncoder), services[3].ImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, services[3].Lifetime);
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
                LanguageViewLocationExpanderFormat.Suffix);

            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer<>), typeof(TestHtmlLocalizer<>)));
            collection.Add(ServiceDescriptor.Transient(typeof(IHtmlLocalizer), typeof(TestViewLocalizer)));
            collection.Add(ServiceDescriptor.Instance(typeof(IHtmlEncoder), typeof(CommonTestEncoder)));

            // Assert
            var services = collection.ToList();
            Assert.Equal(11, services.Count);

            Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), services[0].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[0].Lifetime);
            // REVIEW: What should this be replaced with?
            //Assert.Equal(0, ((IConfigureOptions<RazorViewEngineOptions>)services[0].ImplementationInstance).Order);

            Assert.Equal(typeof(IConfigureOptions<RazorViewEngineOptions>), services[1].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[1].Lifetime);
            // REVIEW: What should this be replaced with?
            //Assert.Equal(-1000, ((IConfigureOptions<RazorViewEngineOptions>)services[1].ImplementationInstance).Order);

            Assert.Equal(typeof(IHtmlLocalizerFactory), services[2].ServiceType);
            Assert.Equal(typeof(HtmlLocalizerFactory), services[2].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[2].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizer<>), services[3].ServiceType);
            Assert.Equal(typeof(HtmlLocalizer<>), services[3].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[3].Lifetime);

            Assert.Equal(typeof(IViewLocalizer), services[4].ServiceType);
            Assert.Equal(typeof(ViewLocalizer), services[4].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[4].Lifetime);

            Assert.Equal(typeof(IHtmlEncoder), services[5].ServiceType);
            Assert.Equal(ServiceLifetime.Singleton, services[5].Lifetime);

            Assert.Equal(typeof(IStringLocalizerFactory), services[6].ServiceType);
            Assert.Equal(typeof(ResourceManagerStringLocalizerFactory), services[6].ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, services[6].Lifetime);

            Assert.Equal(typeof(IStringLocalizer<>), services[7].ServiceType);
            Assert.Equal(typeof(StringLocalizer<>), services[7].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[7].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizer<>), services[8].ServiceType);
            Assert.Equal(typeof(TestHtmlLocalizer<>), services[8].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[8].Lifetime);

            Assert.Equal(typeof(IHtmlLocalizer), services[9].ServiceType);
            Assert.Equal(typeof(TestViewLocalizer), services[9].ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, services[9].Lifetime);

            Assert.Equal(typeof(IHtmlEncoder), services[10].ServiceType);
            Assert.Equal(typeof(CommonTestEncoder), services[10].ImplementationInstance);
            Assert.Equal(ServiceLifetime.Singleton, services[10].Lifetime);
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
