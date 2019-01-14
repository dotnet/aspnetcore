// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
{
    /// <summary>
    /// Describes options for starting the create-react-app server
    /// </summary>
    public class ReactDevelopmentServerMiddlewareOptions
    {
        /// <summary>
        /// The name of the script in your package.json file that launches the create-react-app server.
        /// </summary>
        public string npmScript;
        /// <summary>
        /// Port the create-react-app server should use, if it is not already running.
        /// </summary>
        public int? spaPort;
    }
}