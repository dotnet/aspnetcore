// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    internal static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _failedToCreateXmlSerializer;
        private static readonly Action<ILogger, string, Exception> _failedToCreateDataContractSerializer;

        static LoggerExtensions()
        {
            _failedToCreateXmlSerializer = LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(1, "FailedToCreateXmlSerializer"),
                "An error occurred while trying to create an XmlSerializer for the type '{Type}'.");

            _failedToCreateDataContractSerializer = LoggerMessage.Define<string>(
                LogLevel.Warning,
                new EventId(2, "FailedToCreateDataContractSerializer"),
                "An error occurred while trying to create a DataContractSerializer for the type '{Type}'.");
        }

        public static void FailedToCreateXmlSerializer(this ILogger logger, string typeName, Exception exception)
        {
            _failedToCreateXmlSerializer(logger, typeName, exception);
        }

        public static void FailedToCreateDataContractSerializer(this ILogger logger, string typeName, Exception exception)
        {
            _failedToCreateDataContractSerializer(logger, typeName, exception);
        }
    }
}
