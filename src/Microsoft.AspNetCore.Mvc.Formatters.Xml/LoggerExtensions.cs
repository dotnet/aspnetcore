// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml.Internal
{
    public static class LoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _failedToCreateXmlSerializer;
        private static readonly Action<ILogger, string, Exception> _failedToCreateDataContractSerializer;

        static LoggerExtensions()
        {
            _failedToCreateXmlSerializer = LoggerMessage.Define<string>(
                LogLevel.Warning,
                1,
                "An error occurred while trying to create an XmlSerializer for the type '{Type}'.");

            _failedToCreateDataContractSerializer = LoggerMessage.Define<string>(
                LogLevel.Warning,
                2,
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
