// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.CommandLineUtils
{
    public static class UserSecretsCommandLineExtensions
    {
        public static CommandOption HelpOption(this CommandLineApplication app)
        {
            return app.HelpOption("-?|-h|--help");
        }

        public static void OnExecute(this CommandLineApplication app, Action action)
        {
            app.OnExecute(() =>
            {
                action();
                return 0;
            });
        }
    }
}