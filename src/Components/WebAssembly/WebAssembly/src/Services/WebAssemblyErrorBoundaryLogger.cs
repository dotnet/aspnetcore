// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal class WebAssemblyErrorBoundaryLogger : IErrorBoundaryLogger
    {
        private readonly ILogger<ErrorBoundary> _errorBoundaryLogger;

        public WebAssemblyErrorBoundaryLogger(ILogger<ErrorBoundary> errorBoundaryLogger)
        {
            _errorBoundaryLogger = errorBoundaryLogger ?? throw new ArgumentNullException(nameof(errorBoundaryLogger)); ;
        }

        public ValueTask LogErrorAsync(Exception exception)
        {
            // For, client-side code, all internal state is visible to the end user. We can just
            // log directly to the console.
            _errorBoundaryLogger.LogError(exception.ToString());
            return ValueTask.CompletedTask;
        }
    }
}
