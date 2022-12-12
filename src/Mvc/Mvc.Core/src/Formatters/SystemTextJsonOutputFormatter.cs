// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

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

        if (!jsonSerializerOptions.IsReadOnly &&
            jsonSerializerOptions.TypeInfoResolver != null)
        {
            jsonSerializerOptions.MakeReadOnly();
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
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (selectedEncoding == null)
        {
            throw new ArgumentNullException(nameof(selectedEncoding));
        }

        var httpContext = context.HttpContext;

        // Maybe we could use the jsontypeinfo overload but we need the untyped,
        // waiting for https://github.com/dotnet/runtime/issues/77051

        var declaredType = context.ObjectType;
        var runtimeType = context.Object?.GetType();
        var objectType = runtimeType ?? declaredType ?? typeof(object);

        if (declaredType is not null &&
            runtimeType != declaredType &&
            SerializerOptions.TypeInfoResolver != null &&
            SerializerOptions.GetTypeInfo(declaredType).PolymorphismOptions is not null)
        {
            // Using declared type in this case. The polymorphism is not
            // relevant for us and will be handled by STJ, if needed.
            objectType = declaredType;
        }

        var responseStream = httpContext.Response.Body;
        if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            try
            {
                await JsonSerializer.SerializeAsync(responseStream, context.Object, objectType, SerializerOptions, httpContext.RequestAborted);
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
                await JsonSerializer.SerializeAsync(transcodingStream, context.Object, objectType, SerializerOptions);
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
