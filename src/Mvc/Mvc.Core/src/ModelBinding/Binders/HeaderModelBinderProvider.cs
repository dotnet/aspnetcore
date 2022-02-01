// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding header values.
/// </summary>
public partial class HeaderModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var bindingInfo = context.BindingInfo;
        if (bindingInfo.BindingSource == null ||
            !bindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Header))
        {
            return null;
        }

        var modelMetadata = context.Metadata;
        var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<HeaderModelBinderProvider>();

        if (!IsSimpleType(modelMetadata))
        {
            Log.CannotCreateHeaderModelBinder(logger, modelMetadata.ModelType);
            return null;
        }

        // Since we are delegating the binding of the current model type to other binders, modify the
        // binding source of the current model type to a non-FromHeader binding source in order to avoid an
        // infinite recursion into this binder provider.
        var nestedBindingInfo = new BindingInfo(bindingInfo)
        {
            BindingSource = BindingSource.ModelBinding
        };

        var innerModelBinder = context.CreateBinder(
            modelMetadata.GetMetadataForType(modelMetadata.ModelType),
            nestedBindingInfo);

        if (innerModelBinder == null)
        {
            return null;
        }

        return new HeaderModelBinder(loggerFactory, innerModelBinder);
    }

    // Support binding only to simple types or collection of simple types.
    private static bool IsSimpleType(ModelMetadata modelMetadata)
    {
        var metadata = modelMetadata.ElementMetadata ?? modelMetadata;
        return !metadata.IsComplexType;
    }

    private static partial class Log
    {
        [LoggerMessage(20, LogLevel.Debug, "Could not create a binder for type '{ModelType}' as this binder only supports simple types (like string, int, bool, enum) or a collection of simple types.", EventName = "CannotCreateHeaderModelBinder")]
        public static partial void CannotCreateHeaderModelBinder(ILogger logger, Type modelType);
    }
}
