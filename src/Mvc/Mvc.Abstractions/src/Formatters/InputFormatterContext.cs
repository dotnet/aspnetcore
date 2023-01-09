// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Formatters;

/// <summary>
/// A context object used by an input formatter for deserializing the request body into an object.
/// </summary>
public class InputFormatterContext
{
    /// <summary>
    /// Creates a new instance of <see cref="InputFormatterContext"/>.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="Http.HttpContext"/> for the current operation.
    /// </param>
    /// <param name="modelName">The name of the model.</param>
    /// <param name="modelState">
    /// The <see cref="ModelStateDictionary"/> for recording errors.
    /// </param>
    /// <param name="metadata">
    /// The <see cref="ModelMetadata"/> of the model to deserialize.
    /// </param>
    /// <param name="readerFactory">
    /// A delegate which can create a <see cref="TextReader"/> for the request body.
    /// </param>
    public InputFormatterContext(
        HttpContext httpContext,
        string modelName,
        ModelStateDictionary modelState,
        ModelMetadata metadata,
        Func<Stream, Encoding, TextReader> readerFactory)
        : this(httpContext, modelName, modelState, metadata, readerFactory, treatEmptyInputAsDefaultValue: false)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="InputFormatterContext"/>.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="Http.HttpContext"/> for the current operation.
    /// </param>
    /// <param name="modelName">The name of the model.</param>
    /// <param name="modelState">
    /// The <see cref="ModelStateDictionary"/> for recording errors.
    /// </param>
    /// <param name="metadata">
    /// The <see cref="ModelMetadata"/> of the model to deserialize.
    /// </param>
    /// <param name="readerFactory">
    /// A delegate which can create a <see cref="TextReader"/> for the request body.
    /// </param>
    /// <param name="treatEmptyInputAsDefaultValue">
    /// A value for the <see cref="TreatEmptyInputAsDefaultValue"/> property.
    /// </param>
    public InputFormatterContext(
        HttpContext httpContext,
        string modelName,
        ModelStateDictionary modelState,
        ModelMetadata metadata,
        Func<Stream, Encoding, TextReader> readerFactory,
        bool treatEmptyInputAsDefaultValue)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(modelName);
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(readerFactory);

        HttpContext = httpContext;
        ModelName = modelName;
        ModelState = modelState;
        Metadata = metadata;
        ReaderFactory = readerFactory;
        TreatEmptyInputAsDefaultValue = treatEmptyInputAsDefaultValue;
        ModelType = metadata.ModelType;
    }

    /// <summary>
    /// Gets a flag to indicate whether the input formatter should allow no value to be provided.
    /// If <see langword="false"/>, the input formatter should handle empty input by returning
    /// <see cref="InputFormatterResult.NoValueAsync()"/>. If <see langword="true"/>, the input
    /// formatter should handle empty input by returning the default value for the type
    /// <see cref="ModelType"/>.
    /// </summary>
    public bool TreatEmptyInputAsDefaultValue { get; }

    /// <summary>
    /// Gets the <see cref="Http.HttpContext"/> associated with the current operation.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Gets the name of the model. Used as the key or key prefix for errors added to <see cref="ModelState"/>.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the <see cref="ModelStateDictionary"/> associated with the current operation.
    /// </summary>
    public ModelStateDictionary ModelState { get; }

    /// <summary>
    /// Gets the requested <see cref="ModelMetadata"/> of the request body deserialization.
    /// </summary>
    public ModelMetadata Metadata { get; }

    /// <summary>
    /// Gets the requested <see cref="Type"/> of the request body deserialization.
    /// </summary>
    public Type ModelType { get; }

    /// <summary>
    /// Gets a delegate which can create a <see cref="TextReader"/> for the request body.
    /// </summary>
    public Func<Stream, Encoding, TextReader> ReaderFactory { get; }
}
