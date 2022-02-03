// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A <see cref="IConfigureOptions{TOptions}"/> implementation which will add the
/// XML serializer formatters to <see cref="MvcOptions"/>.
/// </summary>
internal sealed class XmlSerializerMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="XmlSerializerMvcOptionsSetup"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public XmlSerializerMvcOptionsSetup(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Adds the XML serializer formatters to <see cref="MvcOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    public void Configure(MvcOptions options)
    {
        // Do not override any user mapping
        var key = "xml";
        var mapping = options.FormatterMappings.GetMediaTypeMappingForFormat(key);
        if (string.IsNullOrEmpty(mapping))
        {
            options.FormatterMappings.SetMediaTypeMappingForFormat(
                key,
                MediaTypeHeaderValues.ApplicationXml);
        }

        var inputFormatter = new XmlSerializerInputFormatter(options);
        inputFormatter.WrapperProviderFactories.Add(new ProblemDetailsWrapperProviderFactory());
        options.InputFormatters.Add(inputFormatter);

        var outputFormatter = new XmlSerializerOutputFormatter(_loggerFactory);
        outputFormatter.WrapperProviderFactories.Add(new ProblemDetailsWrapperProviderFactory());
        options.OutputFormatters.Add(outputFormatter);
    }
}
