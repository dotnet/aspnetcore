// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    internal static class MvcTagHelperLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _distributedFormatterDeserializedFailed;

        static MvcTagHelperLoggerExtensions()
        {
            _distributedFormatterDeserializedFailed = LoggerMessage.Define<string>(
                LogLevel.Error,
                new EventId(1, "DistributedFormatterDeserializationException"),
                "Couldn't deserialize cached value for key {Key}.");
        }

        public static void DistributedFormatterDeserializationException(this ILogger logger, string key, Exception exception)
        {
            _distributedFormatterDeserializedFailed(logger, key, exception);
        }
    }
}
