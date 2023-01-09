// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for deserializing the request body using a formatter.
/// </summary>
public class BodyModelBinderProvider : IModelBinderProvider
{
    private readonly IList<IInputFormatter> _formatters;
    private readonly IHttpRequestStreamReaderFactory _readerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly MvcOptions? _options;

    /// <summary>
    /// Creates a new <see cref="BodyModelBinderProvider"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
    public BodyModelBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
        : this(formatters, readerFactory, loggerFactory: NullLoggerFactory.Instance)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BodyModelBinderProvider"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    public BodyModelBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory, ILoggerFactory loggerFactory)
        : this(formatters, readerFactory, loggerFactory, options: null)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BodyModelBinderProvider"/>.
    /// </summary>
    /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
    /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    public BodyModelBinderProvider(
        IList<IInputFormatter> formatters,
        IHttpRequestStreamReaderFactory readerFactory,
        ILoggerFactory loggerFactory,
        MvcOptions? options)
    {
        ArgumentNullException.ThrowIfNull(formatters);
        ArgumentNullException.ThrowIfNull(readerFactory);

        _formatters = formatters;
        _readerFactory = readerFactory;
        _loggerFactory = loggerFactory;
        _options = options;
    }

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BindingSource != null &&
            context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Body))
        {
            if (_formatters.Count == 0)
            {
                throw new InvalidOperationException(Resources.FormatInputFormattersAreRequired(
                    typeof(MvcOptions).FullName,
                    nameof(MvcOptions.InputFormatters),
                    typeof(IInputFormatter).FullName));
            }

            var treatEmptyInputAsDefaultValue = CalculateAllowEmptyBody(context.BindingInfo.EmptyBodyBehavior, _options);

            return new BodyModelBinder(_formatters, _readerFactory, _loggerFactory, _options)
            {
                AllowEmptyBody = treatEmptyInputAsDefaultValue,
            };
        }

        return null;
    }

    internal static bool CalculateAllowEmptyBody(EmptyBodyBehavior emptyBodyBehavior, MvcOptions? options)
    {
        if (emptyBodyBehavior == EmptyBodyBehavior.Default)
        {
            return options?.AllowEmptyInputInBodyModelBinding ?? false;
        }

        return emptyBodyBehavior == EmptyBodyBehavior.Allow;
    }
}
