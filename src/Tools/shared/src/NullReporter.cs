// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Tools.Internal
{
    public class NullReporter : IReporter
    {
        private NullReporter()
        { }

        public static IReporter Singleton { get; } = new NullReporter();

        public void Verbose(string message)
        { }

        public void Output(string message)
        { }

        public void Warn(string message)
        { }

        public void Error(string message)
        { }
    }
}
