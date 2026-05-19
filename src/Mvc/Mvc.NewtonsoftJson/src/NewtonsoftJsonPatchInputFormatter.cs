// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A <see cref="TextInputFormatter"/> for JSON Patch (application/json-patch+json) content.
/// </summary>
public class NewtonsoftJsonPatchInputFormatter : NewtonsoftJsonInputFormatter
{
    /// <summary>
    /// Initializes a new <see cref="NewtonsoftJsonPatchInputFormatter"/> instance.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="serializerSettings">
    /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
    /// (<see cref="MvcNewtonsoftJsonOptions.SerializerSettings"/>) or an instance
    /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
    /// </param>
    /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
    /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
    /// <param name="options">The <see cref="MvcOptions"/>.</param>
    /// <param name="jsonOptions">The <see cref="MvcNewtonsoftJsonOptions"/>.</param>
    public NewtonsoftJsonPatchInputFormatter(
        ILogger logger,
        JsonSerializerSettings serializerSettings,
        ArrayPool<char> charPool,
        ObjectPoolProvider objectPoolProvider,
        MvcOptions options,
        MvcNewtonsoftJsonOptions jsonOptions)
        : base(logger, serializerSettings, charPool, objectPoolProvider, options, jsonOptions)
    {
        // Clear all values and only include json-patch+json value.
        SupportedMediaTypes.Clear();

        SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
    }

    /// <inheritdoc />
    public override InputFormatterExceptionPolicy ExceptionPolicy
    {
        get
        {
            if (GetType() == typeof(NewtonsoftJsonPatchInputFormatter))
            {
                return InputFormatterExceptionPolicy.MalformedInputExceptions;
            }
            return InputFormatterExceptionPolicy.AllExceptions;
        }
    }

    /// <inheritdoc />
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        var result = await base.ReadRequestBodyAsync(context, encoding);
        if (!result.HasError)
        {
            if (result.Model is IJsonPatchDocument jsonPatchDocument && SerializerSettings.ContractResolver is not null)
            {
                jsonPatchDocument.ContractResolver = SerializerSettings.ContractResolver;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public override bool CanRead(InputFormatterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.ModelType;
        if (!typeof(IJsonPatchDocument).IsAssignableFrom(modelType) ||
            !modelType.IsGenericType)
        {
            return false;
        }

        return base.CanRead(context);
    }
}
