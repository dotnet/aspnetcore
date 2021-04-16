// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal static class LoggerEventIds
    {
        public static readonly EventId RequestLog = new EventId(1, "RequestLog");
        public static readonly EventId ResponseLog = new EventId(2, "ResponseLog");
        public static readonly EventId RequestBody = new EventId(3, "RequestBody");
        public static readonly EventId ResponseBody = new EventId(4, "ResponseBody");
        public static readonly EventId DecodeFailure = new EventId(5, "DecodeFaulure");
        public static readonly EventId UnrecognizedMediaType = new EventId(6, "UnrecognizedMediaType");
    }
}
