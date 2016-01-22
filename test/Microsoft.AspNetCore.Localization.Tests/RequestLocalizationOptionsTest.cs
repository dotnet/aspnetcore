// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Xunit;

namespace Microsoft.AspNetCore.Localization.Tests
{
    public class RequestLocalizationOptionsTest : IDisposable
    {
        private readonly CultureInfo _initialCulture;
        private readonly CultureInfo _initialUICulture;

        public RequestLocalizationOptionsTest()
        {
            _initialCulture = CultureInfo.CurrentCulture;
            _initialUICulture = CultureInfo.CurrentUICulture;
        }

        [Fact]
        public void DefaultRequestCulture_DefaultsToCurrentCulture()
        {
            // Arrange/Act
            var options = new RequestLocalizationOptions();

            // Assert
            Assert.NotNull(options.DefaultRequestCulture);
            Assert.Equal(CultureInfo.CurrentCulture, options.DefaultRequestCulture.Culture);
            Assert.Equal(CultureInfo.CurrentUICulture, options.DefaultRequestCulture.UICulture);
        }

        [Fact]
        public void DefaultRequestCulture_DefaultsToCurrentCultureWhenExplicitlySet()
        {
            // Arrange
            var explicitCulture = new CultureInfo("fr-FR");
#if DNX451
            Thread.CurrentThread.CurrentCulture = explicitCulture;
            Thread.CurrentThread.CurrentUICulture = explicitCulture;
#else
            CultureInfo.CurrentCulture = explicitCulture;
            CultureInfo.CurrentUICulture = explicitCulture;
#endif
            // Act
            var options = new RequestLocalizationOptions();

            // Assert
            Assert.Equal(explicitCulture, options.DefaultRequestCulture.Culture);
            Assert.Equal(explicitCulture, options.DefaultRequestCulture.UICulture);
        }

        [Fact]
        public void DefaultRequestCulture_ThrowsWhenTryingToSetToNull()
        {
            // Arrange
            var options = new RequestLocalizationOptions();

            // Act/Assert
            Assert.Throws(typeof(ArgumentNullException), () => options.DefaultRequestCulture = null);
        }

        [Fact]
        public void SupportedCultures_DefaultsToCurrentCulture()
        {
            // Arrange/Act
            var options = new RequestLocalizationOptions();

            // Assert
            Assert.Collection(options.SupportedCultures, item => Assert.Equal(CultureInfo.CurrentCulture, item));
            Assert.Collection(options.SupportedUICultures, item => Assert.Equal(CultureInfo.CurrentUICulture, item));
        }

        [Fact]
        public void SupportedCultures_DefaultsToCurrentCultureWhenExplicitlySet()
        {
            // Arrange
            var explicitCulture = new CultureInfo("fr-FR");
#if DNX451
            Thread.CurrentThread.CurrentCulture = explicitCulture;
            Thread.CurrentThread.CurrentUICulture = explicitCulture;
#else
            CultureInfo.CurrentCulture = explicitCulture;
            CultureInfo.CurrentUICulture = explicitCulture;
#endif

            // Act
            var options = new RequestLocalizationOptions();

            // Assert
            Assert.Collection(options.SupportedCultures, item => Assert.Equal(explicitCulture, item));
            Assert.Collection(options.SupportedUICultures, item => Assert.Equal(explicitCulture, item));
        }

        public void Dispose()
        {
#if DNX451
            Thread.CurrentThread.CurrentCulture = _initialCulture;
            Thread.CurrentThread.CurrentUICulture = _initialUICulture;
#else
            CultureInfo.CurrentCulture = _initialCulture;
            CultureInfo.CurrentUICulture = _initialUICulture;
#endif
        }
    }
}
