// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    /// <summary>
    /// Describes how to find the JavaScript code that performs prerendering.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class JavaScriptModuleExport
    {
        /// <summary>
        /// Creates a new instance of <see cref="JavaScriptModuleExport"/>.
        /// </summary>
        /// <param name="moduleName">The path to the JavaScript module containing prerendering code.</param>
        public JavaScriptModuleExport(string moduleName)
        {
            ModuleName = moduleName;
        }

        /// <summary>
        /// Specifies the path to the JavaScript module containing prerendering code.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// If set, specifies the name of the CommonJS export that is the prerendering function to execute.
        /// If not set, the JavaScript module's default CommonJS export must itself be the prerendering function.
        /// </summary>
        public string ExportName { get; set; }
    }
}
