// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    internal static class DiagnosticsEntityFrameworkCoreLoggerExtensions
    {
        // MigrationsEndPointMiddleware
        private static readonly Action<ILogger, Exception> _noContextType = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "NoContextType"),
            "No context type was specified. Ensure the form data from the request includes a 'context' value, specifying the context type name to apply migrations for.");

        private static readonly Action<ILogger, string, Exception> _invalidContextType = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(2, "InvalidContextType"),
            "The context type '{ContextTypeName}' could not be loaded. Ensure this is the correct type name for the context you are trying to apply migrations for.");

        private static readonly Action<ILogger, string, Exception> _contextNotRegistered = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(3, "ContextNotRegistered"),
            "The context type '{ContextTypeName}' was not found in services. This usually means the context was not registered in services during startup. You probably want to call AddScoped<>() inside the UseServices(...) call in your application startup code.");

        private static readonly Action<ILogger, string, Exception> _requestPathMatched = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(4, "RequestPathMatched"),
            "Request path matched the path configured for this migrations endpoint({RequestPath}). Attempting to process the migrations request.");

        private static readonly Action<ILogger, string, Exception> _applyingMigrations = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(5, "ApplyingMigrations"),
            "Request is valid, applying migrations for context '{ContextTypeName}'");

        private static readonly Action<ILogger, string, Exception> _migrationsApplied = LoggerMessage.Define<string>(
            LogLevel.Debug,
            new EventId(6, "MigrationsApplied"),
            "Migrations successfully applied for context '{ContextTypeName}'.");

        private static readonly Action<ILogger, string, Exception> _migrationsEndPointMiddlewareException = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(7, "MigrationsEndPointException"),
            "An error occurred while applying the migrations for '{ContextTypeName}'. See InnerException for details:");

        // DatabaseErrorPageMiddleware
        private static readonly Action<ILogger, Type, Exception> _attemptingToMatchException = LoggerMessage.Define<Type>(
            LogLevel.Debug,
            new EventId(1, "AttemptingToMatchException"),
            "{ExceptionType} occurred, checking if Entity Framework recorded this exception as resulting from a failed database operation.");

        private static readonly Action<ILogger, Exception> _noRecordedException = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(2, "NoRecordedException"),
            "Entity Framework did not record any exceptions due to failed database operations. This means the current exception is not a failed Entity Framework database operation, or the current exception occurred from a DbContext that was not obtained from request services.");

        private static readonly Action<ILogger, Exception> _noMatch = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(3, "NoMatchFound"),
            "The current exception (and its inner exceptions) do not match the last exception Entity Framework recorded due to a failed database operation. This means the database operation exception was handled and another exception occurred later in the request.");

        private static readonly Action<ILogger, Exception> _matched = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(4, "MatchFound"),
            "Entity Framework recorded that the current exception was due to a failed database operation. Attempting to show database error page.");

        private static readonly Action<ILogger, string, Exception> _contextNotRegisteredDatabaseErrorPageMiddleware = LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5, "ContextNotRegistered"),
            "The context type '{ContextTypeName}' was not found in services. This usually means the context was not registered in services during startup. You probably want to call AddScoped<>() inside the UseServices(...) call in your application startup code. Skipping display of the database error page.");

        private static readonly Action<ILogger, Exception> _notRelationalDatabase = LoggerMessage.Define(
            LogLevel.Debug,
            new EventId(6, "NotRelationalDatabase"),
            "The target data store is not a relational database. Skipping the database error page.");

        private static readonly Action<ILogger, Exception> _databaseErrorPageMiddlewareException = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(7, "DatabaseErrorPageException"),
            "An exception occurred while calculating the database error page content. Skipping display of the database error page.");

        public static void NoContextType(this ILogger logger)
        {
            _noContextType(logger, null);
        }

        public static void InvalidContextType(this ILogger logger, string contextTypeName)
        {
            _invalidContextType(logger, contextTypeName, null);
        }

        public static void ContextNotRegistered(this ILogger logger, string contextTypeName)
        {
            _contextNotRegistered(logger, contextTypeName, null);
        }

        public static void RequestPathMatched(this ILogger logger, string requestPath)
        {
            _requestPathMatched(logger, requestPath, null);
        }

        public static void ApplyingMigrations(this ILogger logger, string contextTypeName)
        {
            _applyingMigrations(logger, contextTypeName, null);
        }

        public static void MigrationsApplied(this ILogger logger, string contextTypeName)
        {
            _migrationsApplied(logger, contextTypeName, null);
        }

        public static void MigrationsEndPointMiddlewareException(this ILogger logger, string context, Exception exception)
        {
            _migrationsEndPointMiddlewareException(logger, context, exception);
        }

        public static void AttemptingToMatchException(this ILogger logger, Type exceptionType)
        {
            _attemptingToMatchException(logger, exceptionType, null);
        }

        public static void NoRecordedException(this ILogger logger)
        {
            _noRecordedException(logger, null);
        }

        public static void NoMatch(this ILogger logger)
        {
            _noMatch(logger, null);
        }

        public static void Matched(this ILogger logger)
        {
            _matched(logger, null);
        }

        public static void NotRelationalDatabase(this ILogger logger)
        {
            _notRelationalDatabase(logger, null);
        }

        public static void ContextNotRegisteredDatabaseErrorPageMiddleware(this ILogger logger, string contextTypeName)
        {
            _contextNotRegisteredDatabaseErrorPageMiddleware(logger, contextTypeName, null);
        }

        public static void DatabaseErrorPageMiddlewareException(this ILogger logger, Exception exception)
        {
            _databaseErrorPageMiddlewareException(logger, exception);
        }
    }
}
