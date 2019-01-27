// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SpaServices.Npm;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal static class AngularCliMiddleware
    {
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine 

        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName)
        {
            NpmMiddleware.Attach(spaBuilder, npmScriptName, port => $"--port {port}", port => null,
                new Regex("open your browser on (http\\S+)", RegexOptions.None, RegexMatchTimeout),
                (match, port) => new Uri(match.Groups[1].Value));
        }
    }
}
