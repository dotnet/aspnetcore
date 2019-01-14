// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Describes options for starting the Angular CLI process
    /// </summary>
    public class AngularCliMiddlewareOptions
    {
        /// <summary>
        /// The name of the script in your package.json file that launches the Angular CLI process.
        /// </summary>
        public string npmScript;
        /// <summary>
        /// Port the Angular CLI process should use, if it is not already running.
        /// </summary>
        public int? spaPort;
    }
}