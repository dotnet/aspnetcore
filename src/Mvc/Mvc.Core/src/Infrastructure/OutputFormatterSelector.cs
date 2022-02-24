// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Selects an <see cref="IOutputFormatter"/> to write a response to the current request.
/// </summary>
/// <remarks>
/// <para>
/// The default implementation of <see cref="OutputFormatterSelector"/> provided by ASP.NET Core MVC
/// is <see cref="DefaultOutputFormatterSelector"/>. The <see cref="DefaultOutputFormatterSelector"/> implements
/// MVC's default content negotiation algorithm. This API is designed in a way that can satisfy the contract
/// of <see cref="ObjectResult"/>.
/// </para>
/// <para>
/// The default implementation is controlled by settings on <see cref="MvcOptions"/>, most notably:
/// <see cref="MvcOptions.OutputFormatters"/>, <see cref="MvcOptions.RespectBrowserAcceptHeader"/>, and
/// <see cref="MvcOptions.ReturnHttpNotAcceptable"/>.
/// </para>
/// </remarks>
public abstract class OutputFormatterSelector
{
    /// <summary>
    /// Selects an <see cref="IOutputFormatter"/> to write the response based on the provided values and the current request.
    /// </summary>
    /// <param name="context">The <see cref="OutputFormatterCanWriteContext"/> associated with the current request.</param>
    /// <param name="formatters">A list of formatters to use; this acts as an override to <see cref="MvcOptions.OutputFormatters"/>.</param>
    /// <param name="mediaTypes">A list of media types to use; this acts as an override to the <c>Accept</c> header. </param>
    /// <returns>The selected <see cref="IOutputFormatter"/>, or <c>null</c> if one could not be selected.</returns>
    public abstract IOutputFormatter? SelectFormatter(OutputFormatterCanWriteContext context, IList<IOutputFormatter> formatters, MediaTypeCollection mediaTypes);
}
