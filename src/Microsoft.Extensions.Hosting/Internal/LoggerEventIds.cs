// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting.Internal
{
    internal static class LoggerEventIds
    {
        public const int Starting = 1;
        public const int Started = 2;
        public const int Stopping = 3;
        public const int Stopped = 4;
        public const int StoppedWithException = 5;
        public const int ApplicationStartupException = 6;
        public const int ApplicationStoppingException = 7;
        public const int ApplicationStoppedException = 8;
    }
}
