// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Testing;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyCultureProvider;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class WebAssemblyCultureProviderTest
    {
        [Theory]
        [InlineData("fr-FR", new[] { "fr-FR", "fr" })]
        [InlineData("tzm-Latn-DZ", new[] { "tzm-Latn-DZ", "tzm-Latn", "tzm" })]
        public void GetCultures_ReturnsCultureClosure(string cultureName, string[] expected)
        {
            // Arrange
            var culture = new CultureInfo(cultureName);

            // Act
            var actual = WebAssemblyCultureProvider.GetCultures(culture);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task LoadCurrentCultureResourcesAsync_ReadsAssemblies()
        {
            // Arrange
            using var cultureReplacer = new CultureReplacer("en-GB");
            var invoker = new Mock<IJSUnmarshalledRuntime>();
            invoker.Setup(i => i.InvokeUnmarshalled<string[], object, object, Task<object>>(GetSatelliteAssemblies, new[] { "en-GB", "en" }, null, null))
                .Returns(Task.FromResult<object>(1))
                .Verifiable();

            invoker.Setup(i => i.InvokeUnmarshalled<object, object, object, object[]>(ReadSatelliteAssemblies, null, null, null))
                .Returns(new object[] { File.ReadAllBytes(GetType().Assembly.Location) })
                .Verifiable();

            var loader = new WebAssemblyCultureProvider(invoker.Object, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            // Act
            await loader.LoadCurrentCultureResourcesAsync();

            // Assert
            invoker.Verify();
        }

        [Fact]
        public async Task LoadCurrentCultureResourcesAsync_DoesNotReadAssembliesWhenThereAreNone()
        {
            // Arrange
            using var cultureReplacer = new CultureReplacer("en-GB");
            var invoker = new Mock<IJSUnmarshalledRuntime>();
            invoker.Setup(i => i.InvokeUnmarshalled<string[], object, object, Task<object>>(GetSatelliteAssemblies, new[] { "en-GB", "en" }, null, null))
                .Returns(Task.FromResult<object>(0))
                .Verifiable();

            var loader = new WebAssemblyCultureProvider(invoker.Object, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            // Act
            await loader.LoadCurrentCultureResourcesAsync();

            // Assert
            invoker.Verify(i => i.InvokeUnmarshalled<object, object, object, object[]>(ReadSatelliteAssemblies, null, null, null), Times.Never());
        }

        [Fact]
        public void ThrowIfCultureChangeIsUnsupported_ThrowsIfCulturesAreDifferentAndICUShardingIsUsed()
        {
            // Arrange
            Environment.SetEnvironmentVariable("__BLAZOR_SHARDED_ICU", "1");
            try
            {
                // WebAssembly is initialized with en-US
                var cultureProvider = new WebAssemblyCultureProvider(DefaultWebAssemblyJSRuntime.Instance, new CultureInfo("en-US"), new CultureInfo("en-US"));

                // Culture is changed to fr-FR as part of the app
                using var cultureReplacer = new CultureReplacer("fr-FR");

                var ex = Assert.Throws<InvalidOperationException>(() => cultureProvider.ThrowIfCultureChangeIsUnsupported());
                Assert.Equal("Blazor detected a change in the application's culture that is not supported with the current project configuration. " +
                    "To change culture dynamically during startup, set <BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData> in the application's project file.",
                    ex.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("__BLAZOR_SHARDED_ICU", null);
            }
        }
    }
}
