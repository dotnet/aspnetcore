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

        var runtimeType = context.Object?.GetType();
        JsonTypeInfo? jsonTypeInfo = null;

        if (context.ObjectType is not null)
        {
            var declaredTypeJsonInfo = SerializerOptions.GetTypeInfo(context.ObjectType);

            if (declaredTypeJsonInfo.IsValid(runtimeType))
            {
                jsonTypeInfo = declaredTypeJsonInfo;
            }
        }

        jsonTypeInfo ??= SerializerOptions.GetTypeInfo(runtimeType ?? typeof(object));

        var responseStream = httpContext.Response.Body;
        if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            try
            {
                await JsonSerializer.SerializeAsync(responseStream, context.Object, jsonTypeInfo, httpContext.RequestAborted);
                await responseStream.FlushAsync(httpContext.RequestAborted);
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
                await JsonSerializer.SerializeAsync(transcodingStream, context.Object, jsonTypeInfo);
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
