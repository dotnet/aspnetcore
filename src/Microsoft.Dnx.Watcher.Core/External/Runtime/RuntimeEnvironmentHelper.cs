// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Dnx.Runtime
{
    internal static class RuntimeEnvironmentHelper
    {
        private static Lazy<bool> _isMono = new Lazy<bool>(() =>
            _runtimeEnv.Value.RuntimeType == "Mono");

        private static Lazy<bool> _isWindows = new Lazy<bool>(() =>
            _runtimeEnv.Value.OperatingSystem == "Windows");

        private static Lazy<IRuntimeEnvironment> _runtimeEnv = new Lazy<IRuntimeEnvironment>(() =>
            GetRuntimeEnvironment());

        private static IRuntimeEnvironment GetRuntimeEnvironment()
        {
            var provider = Infrastructure.CallContextServiceLocator.Locator.ServiceProvider;
            var environment = (IRuntimeEnvironment)provider?.GetService(typeof(IRuntimeEnvironment));

            if (environment == null)
            {
                throw new InvalidOperationException("Failed to resolve IRuntimeEnvironment");
            }

            return environment;
        }

        public static IRuntimeEnvironment RuntimeEnvironment
        {
            get
            {
                return _runtimeEnv.Value;
            }
        }

        public static bool IsWindows
        {
            get { return _isWindows.Value; }
        }

        public static bool IsMono
        {
            get { return _isMono.Value; }
        }
    }
}
