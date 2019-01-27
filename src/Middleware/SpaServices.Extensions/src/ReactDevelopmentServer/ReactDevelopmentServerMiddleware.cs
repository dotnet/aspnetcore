// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SpaServices.Npm;

namespace Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
{
    internal static class ReactDevelopmentServerMiddleware
    {
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine 

        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName)
        {
            NpmMiddleware.Attach(spaBuilder, npmScriptName, port => null, EnvVars,
                new Regex("Starting the development server", RegexOptions.None, RegexMatchTimeout),
                (match, port) => new UriBuilder("http", "localhost", port).Uri);

            IDictionary<string, string> EnvVars(int portNumber) =>
                new Dictionary<string, string>
                {
                    {"PORT", portNumber.ToString()},
                    {
                        "BROWSER", "none"
                    } // We don't want create-react-app to open its own extra browser window pointing to the internal dev server port
                };
        }
    }
}
