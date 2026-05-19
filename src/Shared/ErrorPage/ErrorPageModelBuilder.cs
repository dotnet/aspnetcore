// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.StackTrace.Sources;

#nullable enable

namespace Microsoft.AspNetCore.Hosting.Views;

internal static class ErrorPageModelBuilder
{
    public static ErrorPageModel CreateErrorPageModel(
        IFileProvider contentRootFileProvider,
        ILogger? logger,
        bool showDetailedErrors,
        Exception exception)
    {
        var systemRuntimeAssembly = typeof(System.ComponentModel.DefaultValueAttribute).Assembly;
        var assemblyVersion = new AssemblyName(systemRuntimeAssembly.FullName!).Version?.ToString() ?? string.Empty;
        var clrVersion = assemblyVersion;
        var currentAssembly = typeof(ErrorPage).Assembly;
        var currentAssemblyVesion = currentAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

        IEnumerable<ExceptionDetails> errorDetails;
        if (showDetailedErrors)
        {
            var exceptionDetailProvider = new ExceptionDetailsProvider(
                contentRootFileProvider,
                logger,
                sourceCodeLineCount: 6);

            errorDetails = exceptionDetailProvider.GetDetails(exception);
        }
        else
        {
            errorDetails = Array.Empty<ExceptionDetails>();
        }

        var model = new ErrorPageModel(
            errorDetails,
            showDetailedErrors,
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.ProcessArchitecture.ToString(),
            clrVersion,
            currentAssemblyVesion,
            RuntimeInformation.OSDescription);
        return model;
    }
}
