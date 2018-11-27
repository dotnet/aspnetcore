// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    public class MvcXmlDataContractSerializerMvcOptionsSetupTest
    {
        [Fact]
        public void AddsFormatterMapping()
        {
            // Arrange
            var optionsSetup = new MvcXmlDataContractSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
            var options = new MvcOptions();

            // Act
            optionsSetup.Configure(options);

            // Assert
            var mappedContentType = options.FormatterMappings.GetMediaTypeMappingForFormat("xml");
            Assert.Equal("application/xml", mappedContentType);
        }

        [Fact]
        public void DoesNotOverrideExistingMapping()
        {
            // Arrange
            var optionsSetup = new MvcXmlDataContractSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
            var options = new MvcOptions();
            options.FormatterMappings.SetMediaTypeMappingForFormat("xml", "text/xml");

            // Act
            optionsSetup.Configure(options);

            // Assert
            var mappedContentType = options.FormatterMappings.GetMediaTypeMappingForFormat("xml");
            Assert.Equal("text/xml", mappedContentType);
        }
    }
}
