// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Localization
{
    public class MvcLocalizationServicesTest
    {
        [Fact]
        public void AddLocalizationServices_AddsNeededServices()
        {
            // Arrange
            var collection = new ServiceCollection();

            // Act
            MvcLocalizationServices.AddMvcViewLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix);

            // Assert
            AssertContainsSingle(collection, typeof(IHtmlLocalizerFactory), typeof(HtmlLocalizerFactory));
            AssertContainsSingle(collection, typeof(IHtmlLocalizer<>), typeof(HtmlLocalizer<>));
            AssertContainsSingle(collection, typeof(IViewLocalizer), typeof(ViewLocalizer));
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

            MvcLocalizationServices.AddMvcViewLocalizationServices(
                collection,
                LanguageViewLocationExpanderFormat.Suffix);

            AssertContainsSingle(collection, typeof(IHtmlLocalizerFactory), typeof(TestHtmlLocalizerFactory));
            AssertContainsSingle(collection, typeof(IHtmlLocalizer<>), typeof(TestHtmlLocalizer<>));
            AssertContainsSingle(collection, typeof(IViewLocalizer), typeof(TestViewLocalizer));
        }

        private void AssertContainsSingle(
            IServiceCollection services,
            Type serviceType,
            Type implementationType)
        {
            var matches = services
                .Where(sd =>
                    sd.ServiceType == serviceType &&
                    sd.ImplementationType == implementationType)
                .ToArray();

            if (matches.Length == 0)
            {
                Assert.True(
                    false,
                    $"Could not find an instance of {implementationType} registered as {serviceType}");
            }
            else if (matches.Length > 1)
            {
                Assert.True(
                    false,
                    $"Found multiple instances of {implementationType} registered as {serviceType}");
            }
        }

        public class TestViewLocalizer : IViewLocalizer
        {
            public LocalizedHtmlString this[string name] => throw new NotImplementedException();

            public LocalizedHtmlString this[string name, params object[] arguments]
                => throw new NotImplementedException();

            public LocalizedString GetString(string name)
            {
                throw new NotImplementedException();
            }

            public LocalizedString GetString(string name, params object[] arguments)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                throw new NotImplementedException();
            }

            public IHtmlLocalizer WithCulture(CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class TestHtmlLocalizer<HomeController> : IHtmlLocalizer<HomeController>
        {
            public LocalizedHtmlString this[string name] => throw new NotImplementedException();

            public LocalizedHtmlString this[string name, params object[] arguments]
                => throw new NotImplementedException();

            public LocalizedString GetString(string name)
            {
                throw new NotImplementedException();
            }

            public LocalizedString GetString(string name, params object[] arguments)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            {
                throw new NotImplementedException();
            }

            public IHtmlLocalizer WithCulture(CultureInfo culture)
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
    }
}
