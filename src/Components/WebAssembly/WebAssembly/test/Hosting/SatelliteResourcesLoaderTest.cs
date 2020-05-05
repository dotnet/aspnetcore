// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;
using static Microsoft.AspNetCore.Components.WebAssembly.Hosting.SatelliteResourcesLoader;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public class SatelliteResourcesLoaderTest
    {
        [Theory]
        [InlineData("fr-FR", new[] { "fr-FR", "fr" })]
        [InlineData("tzm-Latn-DZ", new[] { "tzm-Latn-DZ", "tzm-Latn", "tzm" })]
        public void GetCultures_ReturnsCultureClosure(string cultureName, string[] expected)
        {
            // Arrange
            var culture = new CultureInfo(cultureName);

            // Act
            var actual = SatelliteResourcesLoader.GetCultures(culture);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task LoadCurrentCultureResourcesAsync_ReadsAssemblies()
        {
            // Arrange
            using var cultureReplacer = new CultureReplacer("en-GB");
            var invoker = new Mock<WebAssemblyJSRuntimeInvoker>();
            invoker.Setup(i => i.InvokeUnmarshalled<string[], object, object, Task<object>>(GetSatelliteAssemblies, new[] { "en-GB", "en" }, null, null))
                .Returns(Task.FromResult<object>(1))
                .Verifiable();

            invoker.Setup(i => i.InvokeUnmarshalled<object, object, object, object[]>(ReadSatelliteAssemblies, null, null, null))
                .Returns(new object[] { File.ReadAllBytes(GetType().Assembly.Location) })
                .Verifiable();

            var loader = new SatelliteResourcesLoader(invoker.Object);

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
            var invoker = new Mock<WebAssemblyJSRuntimeInvoker>();
            invoker.Setup(i => i.InvokeUnmarshalled<string[], object, object, Task<object>>(GetSatelliteAssemblies, new[] { "en-GB", "en" }, null, null))
                .Returns(Task.FromResult<object>(0))
                .Verifiable();

            var loader = new SatelliteResourcesLoader(invoker.Object);

            // Act
            await loader.LoadCurrentCultureResourcesAsync();

            // Assert
            invoker.Verify(i => i.InvokeUnmarshalled<object, object, object, object[]>(ReadSatelliteAssemblies, null, null, null), Times.Never());
        }
    }
}
