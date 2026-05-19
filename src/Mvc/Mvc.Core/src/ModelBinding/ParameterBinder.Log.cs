// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public partial class ParameterBinder
{
    private static partial class Log
    {
        public static void AttemptingToBindParameterOrProperty(
            ILogger logger,
            ParameterDescriptor parameter,
            ModelMetadata modelMetadata)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            switch (modelMetadata.MetadataKind)
            {
                case ModelMetadataKind.Parameter:
                    AttemptingToBindParameter(logger, modelMetadata.ParameterName, modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Property:
                    AttemptingToBindProperty(
                        logger,
                        modelMetadata.ContainerType,
                        modelMetadata.PropertyName,
                        modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Type:
                    if (parameter is ControllerParameterDescriptor parameterDescriptor)
                    {
                        AttemptingToBindParameter(
                            logger,
                            parameterDescriptor.ParameterInfo.Name,
                            modelMetadata.ModelType);
                    }
                    else
                    {
                        // Likely binding a page handler parameter. Due to various special cases, parameter.Name may
                        // be empty. No way to determine actual name.
                        AttemptingToBindParameter(logger, parameter.Name, modelMetadata.ModelType);
                    }
                    break;
            }
        }

        [LoggerMessage(22, LogLevel.Debug, "Attempting to bind parameter '{ParameterName}' of type '{ModelType}' ...", EventName = "AttemptingToBindParameter", SkipEnabledCheck = true)]
        private static partial void AttemptingToBindParameter(ILogger logger, string? parameterName, Type modelType);

        [LoggerMessage(39, LogLevel.Debug, "Attempting to bind property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}' ...", EventName = "AttemptingToBindProperty", SkipEnabledCheck = true)]
        private static partial void AttemptingToBindProperty(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType);

        public static void DoneAttemptingToBindParameterOrProperty(
           ILogger logger,
           ParameterDescriptor parameter,
           ModelMetadata modelMetadata)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            switch (modelMetadata.MetadataKind)
            {
                case ModelMetadataKind.Parameter:
                    DoneAttemptingToBindParameter(logger, modelMetadata.ParameterName, modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Property:
                    DoneAttemptingToBindProperty(
                        logger,
                        modelMetadata.ContainerType,
                        modelMetadata.PropertyName,
                        modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Type:
                    if (parameter is ControllerParameterDescriptor parameterDescriptor)
                    {
                        DoneAttemptingToBindParameter(
                            logger,
                            parameterDescriptor.ParameterInfo.Name,
                            modelMetadata.ModelType);
                    }
                    else
                    {
                        // Likely binding a page handler parameter. Due to various special cases, parameter.Name may
                        // be empty. No way to determine actual name.
                        DoneAttemptingToBindParameter(logger, parameter.Name, modelMetadata.ModelType);
                    }
                    break;
            }
        }

        [LoggerMessage(23, LogLevel.Debug, "Done attempting to bind parameter '{ParameterName}' of type '{ModelType}'.", EventName = "DoneAttemptingToBindParameter", SkipEnabledCheck = true)]
        private static partial void DoneAttemptingToBindParameter(ILogger logger, string? parameterName, Type modelType);

        [LoggerMessage(40, LogLevel.Debug, "Done attempting to bind property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}'.", EventName = "DoneAttemptingToBindProperty", SkipEnabledCheck = true)]
        private static partial void DoneAttemptingToBindProperty(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType);

        public static void AttemptingToValidateParameterOrProperty(
            ILogger logger,
            ParameterDescriptor parameter,
            ModelMetadata modelMetadata)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            switch (modelMetadata.MetadataKind)
            {
                case ModelMetadataKind.Parameter:
                    AttemptingToValidateParameter(logger, modelMetadata.ParameterName, modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Property:
                    AttemptingToValidateProperty(
                        logger,
                        modelMetadata.ContainerType,
                        modelMetadata.PropertyName,
                        modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Type:
                    if (parameter is ControllerParameterDescriptor parameterDescriptor)
                    {
                        AttemptingToValidateParameter(
                            logger,
                            parameterDescriptor.ParameterInfo.Name,
                            modelMetadata.ModelType);
                    }
                    else
                    {
                        // Likely binding a page handler parameter. Due to various special cases, parameter.Name may
                        // be empty. No way to determine actual name. This case is less likely than for binding logging
                        // (above). Should occur only with a legacy IModelMetadataProvider implementation.
                        AttemptingToValidateParameter(logger, parameter.Name, modelMetadata.ModelType);
                    }
                    break;
            }
        }

        [LoggerMessage(26, LogLevel.Debug, "Attempting to validate the bound parameter '{ParameterName}' of type '{ModelType}' ...", EventName = "AttemptingToValidateParameter", SkipEnabledCheck = true)]
        private static partial void AttemptingToValidateParameter(ILogger logger, string? parameterName, Type modelType);

        [LoggerMessage(41, LogLevel.Debug, "Attempting to validate the bound property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}' ...", EventName = "AttemptingToValidateProperty", SkipEnabledCheck = true)]
        private static partial void AttemptingToValidateProperty(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType);

        public static void DoneAttemptingToValidateParameterOrProperty(
            ILogger logger,
            ParameterDescriptor parameter,
            ModelMetadata modelMetadata)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            switch (modelMetadata.MetadataKind)
            {
                case ModelMetadataKind.Parameter:
                    DoneAttemptingToValidateParameter(
                        logger,
                        modelMetadata.ParameterName,
                        modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Property:
                    DoneAttemptingToValidateProperty(
                        logger,
                        modelMetadata.ContainerType,
                        modelMetadata.PropertyName,
                        modelMetadata.ModelType);
                    break;
                case ModelMetadataKind.Type:
                    if (parameter is ControllerParameterDescriptor parameterDescriptor)
                    {
                        DoneAttemptingToValidateParameter(
                            logger,
                            parameterDescriptor.ParameterInfo.Name,
                            modelMetadata.ModelType);
                    }
                    else
                    {
                        // Likely binding a page handler parameter. Due to various special cases, parameter.Name may
                        // be empty. No way to determine actual name. This case is less likely than for binding logging
                        // (above). Should occur only with a legacy IModelMetadataProvider implementation.
                        DoneAttemptingToValidateParameter(logger, parameter.Name, modelMetadata.ModelType);
                    }
                    break;
            }
        }

        [LoggerMessage(27, LogLevel.Debug, "Done attempting to validate the bound parameter '{ParameterName}' of type '{ModelType}'.", EventName = "DoneAttemptingToValidateParameter")]
        private static partial void DoneAttemptingToValidateParameter(ILogger logger, string? parameterName, Type modelType);

        [LoggerMessage(42, LogLevel.Debug, "Done attempting to validate the bound property '{PropertyContainerType}.{PropertyName}' of type '{ModelType}'.", EventName = "DoneAttemptingToValidateProperty")]
        private static partial void DoneAttemptingToValidateProperty(ILogger logger, Type? propertyContainerType, string? propertyName, Type modelType);

        public static void ParameterBinderRequestPredicateShortCircuit(
            ILogger logger,
            ParameterDescriptor parameter,
            ModelMetadata modelMetadata)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            switch (modelMetadata.MetadataKind)
            {
                case ModelMetadataKind.Parameter:
                    ParameterBinderRequestPredicateShortCircuitOfParameter(
                        logger,
                        modelMetadata.ParameterName);
                    break;
                case ModelMetadataKind.Property:
                    ParameterBinderRequestPredicateShortCircuitOfProperty(
                        logger,
                        modelMetadata.ContainerType,
                        modelMetadata.PropertyName);
                    break;
                case ModelMetadataKind.Type:
                    if (parameter is ControllerParameterDescriptor controllerParameterDescriptor)
                    {
                        ParameterBinderRequestPredicateShortCircuitOfParameter(
                            logger,
                            controllerParameterDescriptor.ParameterInfo.Name);
                    }
                    else
                    {
                        // Likely binding a page handler parameter. Due to various special cases, parameter.Name may
                        // be empty. No way to determine actual name. This case is less likely than for binding logging
                        // (above). Should occur only with a legacy IModelMetadataProvider implementation.
                        ParameterBinderRequestPredicateShortCircuitOfParameter(logger, parameter.Name);
                    }
                    break;
            }
        }

        [LoggerMessage(47, LogLevel.Debug, "Skipped binding property '{PropertyContainerType}.{PropertyName}' since its binding information disallowed it for the current request.",
            EventName = "ParameterBinderRequestPredicateShortCircuitOfProperty",
            SkipEnabledCheck = true)]
        private static partial void ParameterBinderRequestPredicateShortCircuitOfProperty(ILogger logger, Type? propertyContainerType, string? propertyName);

        [LoggerMessage(48, LogLevel.Debug, "Skipped binding parameter '{ParameterName}' since its binding information disallowed it for the current request.",
            EventName = "ParameterBinderRequestPredicateShortCircuitOfParameter",
            SkipEnabledCheck = true)]
        private static partial void ParameterBinderRequestPredicateShortCircuitOfParameter(ILogger logger, string? parameterName);
    }
}
