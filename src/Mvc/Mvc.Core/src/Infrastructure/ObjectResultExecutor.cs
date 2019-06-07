// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Executes an <see cref="ObjectResult"/> to write to the response.
    /// </summary>
    public class ObjectResultExecutor : IActionResultExecutor<ObjectResult>
    {
        private delegate Task<object> ReadAsyncEnumerableDelegate(object value);

        private readonly MethodInfo Converter = typeof(ObjectResultExecutor).GetMethod(
            nameof(ReadAsyncEnumerable),
            BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly ConcurrentDictionary<Type, ReadAsyncEnumerableDelegate> _asyncEnumerableConverters =
            new ConcurrentDictionary<Type, ReadAsyncEnumerableDelegate>();
        private readonly MvcOptions _mvcOptions;

        /// <summary>
        /// Creates a new <see cref="ObjectResultExecutor"/>.
        /// </summary>
        /// <param name="formatterSelector">The <see cref="OutputFormatterSelector"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future release.")]
        public ObjectResultExecutor(
            OutputFormatterSelector formatterSelector,
            IHttpResponseStreamWriterFactory writerFactory,
            ILoggerFactory loggerFactory)
            : this(formatterSelector, writerFactory, loggerFactory, mvcOptions: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ObjectResultExecutor"/>.
        /// </summary>
        /// <param name="formatterSelector">The <see cref="OutputFormatterSelector"/>.</param>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        public ObjectResultExecutor(
            OutputFormatterSelector formatterSelector,
            IHttpResponseStreamWriterFactory writerFactory,
            ILoggerFactory loggerFactory,
            IOptions<MvcOptions> mvcOptions)
        {
            if (formatterSelector == null)
            {
                throw new ArgumentNullException(nameof(formatterSelector));
            }

            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            FormatterSelector = formatterSelector;
            WriterFactory = writerFactory.CreateWriter;
            Logger = loggerFactory.CreateLogger<ObjectResultExecutor>();
            _mvcOptions = mvcOptions?.Value ?? throw new ArgumentNullException(nameof(mvcOptions));
        }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="OutputFormatterSelector"/>.
        /// </summary>
        protected OutputFormatterSelector FormatterSelector { get; }

        /// <summary>
        /// Gets the writer factory delegate.
        /// </summary>
        protected Func<Stream, Encoding, TextWriter> WriterFactory { get; }

        /// <summary>
        /// Executes the <see cref="ObjectResult"/>.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> for the current request.</param>
        /// <param name="result">The <see cref="ObjectResult"/>.</param>
        /// <returns>
        /// A <see cref="Task"/> which will complete once the <see cref="ObjectResult"/> is written to the response.
        /// </returns>
        public virtual Task ExecuteAsync(ActionContext context, ObjectResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            InferContentTypes(context, result);

            var objectType = result.DeclaredType;

            if (objectType == null || objectType == typeof(object))
            {
                objectType = result.Value?.GetType();
            }

            var value = result.Value;

            if (value is IAsyncEnumerable<object> asyncEnumerable)
            {
                return ExecuteAsyncEnumerable(context, result, asyncEnumerable);
            }

            return ExecuteAsyncCore(context, result, objectType, value);
        }

        private async Task ExecuteAsyncEnumerable(ActionContext context, ObjectResult result, IAsyncEnumerable<object> asyncEnumerable)
        {
            var enumerated = await EnumerateAsyncEnumerable(asyncEnumerable);
            await ExecuteAsyncCore(context, result, enumerated.GetType(), enumerated);
        }

        private Task ExecuteAsyncCore(ActionContext context, ObjectResult result, Type objectType, object value)
        {
            var formatterContext = new OutputFormatterWriteContext(
                context.HttpContext,
                WriterFactory,
                objectType,
                value);

            var selectedFormatter = FormatterSelector.SelectFormatter(
                formatterContext,
                (IList<IOutputFormatter>)result.Formatters ?? Array.Empty<IOutputFormatter>(),
                result.ContentTypes);
            if (selectedFormatter == null)
            {
                // No formatter supports this.
                Logger.NoFormatter(formatterContext);

                context.HttpContext.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                return Task.CompletedTask;
            }

            Logger.ObjectResultExecuting(result.Value);

            result.OnFormatting(context);
            return selectedFormatter.WriteAsync(formatterContext);
        }

        private static void InferContentTypes(ActionContext context, ObjectResult result)
        {
            Debug.Assert(result.ContentTypes != null);
            if (result.ContentTypes.Count != 0)
            {
                return;
            }

            // If the user sets the content type both on the ObjectResult (example: by Produces) and Response object,
            // then the one set on ObjectResult takes precedence over the Response object
            var responseContentType = context.HttpContext.Response.ContentType;
            if (!string.IsNullOrEmpty(responseContentType))
            {
                result.ContentTypes.Add(responseContentType);
            }
            else if (result.Value is ProblemDetails)
            {
                result.ContentTypes.Add("application/problem+json");
                result.ContentTypes.Add("application/problem+xml");
            }
        }

        private Task<object> EnumerateAsyncEnumerable(IAsyncEnumerable<object> value)
        {
            var type = value.GetType();
            if (!_asyncEnumerableConverters.TryGetValue(type, out var result))
            {
                var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IAsyncEnumerable<>));
                result = null;
                if (enumerableType != null)
                {
                    var enumeratedObjectType = enumerableType.GetGenericArguments()[0];

                    var converter = (ReadAsyncEnumerableDelegate)Converter
                        .MakeGenericMethod(enumeratedObjectType)
                        .CreateDelegate(typeof(ReadAsyncEnumerableDelegate), this);

                    _asyncEnumerableConverters.TryAdd(type, converter);
                    result = converter;
                }
            }

            return result(value);
        }

        private async Task<object> ReadAsyncEnumerable<T>(object value)
        {
            var asyncEnumerable = (IAsyncEnumerable<T>)value;
            var result = new List<T>();
            var count = 0;

            await foreach (var item in asyncEnumerable)
            {
                if (count++ >= _mvcOptions.MaxIAsyncEnumerableBufferLimit)
                {
                    throw new InvalidOperationException(Resources.FormatObjectResultExecutor_MaxEnumerationExceeded(
                        nameof(ObjectResultExecutor),
                        _mvcOptions.MaxIAsyncEnumerableBufferLimit,
                        value.GetType()));
                }

                result.Add(item);
            }

            return result;
        }
    }
}
