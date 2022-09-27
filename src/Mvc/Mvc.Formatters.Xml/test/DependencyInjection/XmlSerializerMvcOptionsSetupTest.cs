// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

public class XmlSerializerMvcOptionsSetupTest
{
    [Fact]
    public void AddsFormatterMapping()
    {
        // Arrange
        var optionsSetup = new XmlSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
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
        var optionsSetup = new XmlSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
        var options = new MvcOptions();
        options.FormatterMappings.SetMediaTypeMappingForFormat("xml", "text/xml");

        // Act
        optionsSetup.Configure(options);

        // Assert
        var mappedContentType = options.FormatterMappings.GetMediaTypeMappingForFormat("xml");
        Assert.Equal("text/xml", mappedContentType);
    }

    [Fact]
    public void AddsInputFormatter()
    {
        // Arrange
        var optionsSetup = new XmlSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
        var options = new MvcOptions();

        // Act
        optionsSetup.Configure(options);

        // Assert
        Assert.IsType<XmlSerializerInputFormatter>(Assert.Single(options.InputFormatters));
    }

    [Fact]
    public void AddsOutputFormatter()
    {
        // Arrange
        var optionsSetup = new XmlSerializerMvcOptionsSetup(NullLoggerFactory.Instance);
        var options = new MvcOptions();

        // Act
        optionsSetup.Configure(options);

        // Assert
        Assert.IsType<XmlSerializerOutputFormatter>(Assert.Single(options.OutputFormatters));
    }
}
