// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SpaServices.Webpack
{
    /// <summary>
    /// Options for configuring a Webpack dev middleware compiler.
    /// </summary>
    [Obsolete("Use Microsoft.AspNetCore.SpaServices.Extensions")]
    public class WebpackDevMiddlewareOptions
    {
        /// <summary>
        /// If true, hot module replacement (HMR) will be enabled. This automatically updates Webpack-built
        /// resources (such as JavaScript, CSS, or images) in your web browser whenever source files are changed.
        /// </summary>
        public bool HotModuleReplacement { get; set; }

        /// <summary>
        /// If set, overrides the URL that Webpack's client-side code will connect to when listening for updates.
        /// This must be a root-relative URL similar to "/__webpack_hmr" (which is the default endpoint).
        /// </summary>
        public string HotModuleReplacementEndpoint { get; set; }

        /// <summary>
        /// Overrides the internal port number that client-side HMR code will connect to.
        /// </summary>
        public int HotModuleReplacementServerPort { get; set; }

        /// <summary>
        /// If true, enables React-specific extensions to Webpack's hot module replacement (HMR) feature.
        /// This enables React components to be updated without losing their in-memory state.
        /// </summary>
        public bool ReactHotModuleReplacement { get; set; }

        /// <summary>
        /// Specifies additional options to be passed to the Webpack Hot Middleware client, if used.
        /// </summary>
        public IDictionary<string, string> HotModuleReplacementClientOptions { get; set; }

        /// <summary>
        /// Specifies the Webpack configuration file to be used. If not set, defaults to 'webpack.config.js'.
        /// </summary>
        public string ConfigFile { get; set; }

        /// <summary>
        /// The root path of your project. Webpack runs in this context.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// Specifies additional environment variables to be passed to the Node instance hosting
        /// the webpack compiler.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Specifies a value for the "env" parameter to be passed into the Webpack configuration
        /// function. The value must be JSON-serializable, and will only be used if the Webpack
        /// configuration is exported as a function.
        /// </summary>
        public object EnvParam { get; set; }
    }
}
