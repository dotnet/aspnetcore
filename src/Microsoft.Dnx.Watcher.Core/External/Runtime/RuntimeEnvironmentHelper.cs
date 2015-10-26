// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.PlatformAbstractions;

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
            var environment = PlatformServices.Default.Runtime;

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
