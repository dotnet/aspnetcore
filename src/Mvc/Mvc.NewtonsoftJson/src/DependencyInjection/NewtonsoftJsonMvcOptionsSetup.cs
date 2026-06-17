// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up JSON formatter options for <see cref="MvcOptions"/>.
/// </summary>
internal sealed class NewtonsoftJsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MvcNewtonsoftJsonOptions _jsonOptions;
    private readonly ArrayPool<char> _charPool;
    private readonly ObjectPoolProvider _objectPoolProvider;

    public NewtonsoftJsonMvcOptionsSetup(
        ILoggerFactory loggerFactory,
        IOptions<MvcNewtonsoftJsonOptions> jsonOptions,
        ArrayPool<char> charPool,
        ObjectPoolProvider objectPoolProvider)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(jsonOptions);
        ArgumentNullException.ThrowIfNull(charPool);
        ArgumentNullException.ThrowIfNull(objectPoolProvider);

        _loggerFactory = loggerFactory;
        _jsonOptions = jsonOptions.Value;
        _charPool = charPool;
        _objectPoolProvider = objectPoolProvider;
    }

    public void Configure(MvcOptions options)
    {
        options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
        options.OutputFormatters.Add(new NewtonsoftJsonOutputFormatter(_jsonOptions.SerializerSettings, _charPool, options, _jsonOptions));

        options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
        // Register JsonPatchInputFormatter before JsonInputFormatter, otherwise
        // JsonInputFormatter would consume "application/json-patch+json" requests
        // before JsonPatchInputFormatter gets to see them.
        var jsonInputPatchLogger = _loggerFactory.CreateLogger(typeof(NewtonsoftJsonPatchInputFormatter));
        options.InputFormatters.Add(new NewtonsoftJsonPatchInputFormatter(
            jsonInputPatchLogger,
            _jsonOptions.SerializerSettings,
            _charPool,
            _objectPoolProvider,
            options,
            _jsonOptions));

        var jsonInputLogger = _loggerFactory.CreateLogger(typeof(NewtonsoftJsonInputFormatter));
        options.InputFormatters.Add(new NewtonsoftJsonInputFormatter(
            jsonInputLogger,
            _jsonOptions.SerializerSettings,
            _charPool,
            _objectPoolProvider,
            options,
            _jsonOptions));

        options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValues.ApplicationJson);

        options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IJsonPatchDocument)));
        options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
    }
}
