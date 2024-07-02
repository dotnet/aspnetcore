// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextOutputFormatter"/> for JSON content that uses <see cref="JsonSerializer"/>.
/// </summary>
public class SystemTextJsonOutputFormatter : TextOutputFormatter
{
    /// <summary>
    /// Initializes a new <see cref="SystemTextJsonOutputFormatter"/> instance.
    /// </summary>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/>.</param>
    public SystemTextJsonOutputFormatter(JsonSerializerOptions jsonSerializerOptions)
    {
        SerializerOptions = jsonSerializerOptions;

        jsonSerializerOptions.MakeReadOnly();

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
    }

    internal static SystemTextJsonOutputFormatter CreateFormatter(JsonOptions jsonOptions)
    {
        var jsonSerializerOptions = jsonOptions.JsonSerializerOptions;

        if (jsonSerializerOptions.Encoder is null)
        {
            // If the user hasn't explicitly configured the encoder, use the less strict encoder that does not encode all non-ASCII characters.
            jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions)
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        return new SystemTextJsonOutputFormatter(jsonSerializerOptions);
    }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used to configure the <see cref="JsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// A single instance of <see cref="SystemTextJsonOutputFormatter"/> is used for all JSON formatting. Any
    /// changes to the options will affect all output formatting.
    /// </remarks>
    public JsonSerializerOptions SerializerOptions { get; }

    /// <inheritdoc />
    public sealed override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(selectedEncoding);

        var httpContext = context.HttpContext;

        // context.ObjectType reflects the declared model type when specified.
        // For polymorphic scenarios where the user declares a return type, but returns a derived type,
        // we want to serialize all the properties on the derived type. This keeps parity with
        // the behavior you get when the user does not declare the return type.
        // To enable this our best option is to check if the JsonTypeInfo for the declared type is valid,
        // if it is use it. If it isn't, serialize the value as 'object' and let JsonSerializer serialize it as necessary.
        JsonTypeInfo? jsonTypeInfo = null;
        if (context.ObjectType is not null)
        {
            var declaredTypeJsonInfo = SerializerOptions.GetTypeInfo(context.ObjectType);

            var runtimeType = context.Object?.GetType();
            if (declaredTypeJsonInfo.ShouldUseWith(runtimeType))
            {
                jsonTypeInfo = declaredTypeJsonInfo;
            }
        }

        if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            try
            {
                var responseWriter = httpContext.Response.BodyWriter;
                if (!httpContext.Response.HasStarted)
                {
                    // Flush headers before starting Json serialization. This avoids an extra layer of buffering before the first flush.
                    await httpContext.Response.StartAsync();
                }

                if (jsonTypeInfo is not null)
                {
                    await JsonSerializer.SerializeAsync(responseWriter, context.Object, jsonTypeInfo, httpContext.RequestAborted);
                }
                else
                {
                    await JsonSerializer.SerializeAsync(responseWriter, context.Object, SerializerOptions, httpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested) { }
        }
        else
        {
            // JsonSerializer only emits UTF8 encoded output, but we need to write the response in the encoding specified by
            // selectedEncoding
            var transcodingStream = Encoding.CreateTranscodingStream(httpContext.Response.Body, selectedEncoding, Encoding.UTF8, leaveOpen: true);

            ExceptionDispatchInfo? exceptionDispatchInfo = null;
            try
            {
                if (jsonTypeInfo is not null)
                {
                    await JsonSerializer.SerializeAsync(transcodingStream, context.Object, jsonTypeInfo);
                }
                else
                {
                    await JsonSerializer.SerializeAsync(transcodingStream, context.Object, SerializerOptions);
                }

                await transcodingStream.FlushAsync();
            }
            catch (Exception ex)
            {
                // TranscodingStream may write to the inner stream as part of it's disposal.
                // We do not want this exception "ex" to be eclipsed by any exception encountered during the write. We will stash it and
                // explicitly rethrow it during the finally block.
                exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                try
                {
                    await transcodingStream.DisposeAsync();
                }
                catch when (exceptionDispatchInfo != null)
                {
                }

                exceptionDispatchInfo?.Throw();
            }
        }
    }
}
