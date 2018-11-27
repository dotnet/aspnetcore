// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    public class RazorConfigurationSerializationTest
    {
        public RazorConfigurationSerializationTest()
        {
            var converters = new JsonConverterCollection();
            converters.RegisterRazorConverters();
            Converters = converters.ToArray();
        }

        public JsonConverter[] Converters { get; }

        [Fact]
        public void RazorConfigurationJsonConverter_Serialization_CanRoundTrip()
        {
            // Arrange
            var configuration = new ProjectSystemRazorConfiguration(
                RazorLanguageVersion.Version_1_1,
                "Test",
                new[]
                {
                    new ProjectSystemRazorExtension("Test-Extension1"),
                    new ProjectSystemRazorExtension("Test-Extension2"),
                });

            // Act
            var json = JsonConvert.SerializeObject(configuration, Converters);
            var obj = JsonConvert.DeserializeObject<RazorConfiguration>(json, Converters);

            // Assert
            Assert.Equal(configuration.ConfigurationName, obj.ConfigurationName);
            Assert.Collection(
                configuration.Extensions, 
                e => Assert.Equal("Test-Extension1", e.ExtensionName),
                e => Assert.Equal("Test-Extension2", e.ExtensionName));
            Assert.Equal(configuration.LanguageVersion, obj.LanguageVersion);
        }
    }
}
