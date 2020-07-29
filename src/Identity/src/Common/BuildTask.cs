// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace Microsoft.DotNet.Build.Tasks
{
    public abstract partial class BuildTask : ITask
    {
        private Log _log = null;

        internal Log Log
        {
            get { return _log ?? (_log = new Log(new TaskLoggingHelper(this))); }
        }

        public BuildTask()
        {
        }

        public IBuildEngine BuildEngine
        {
            get;
            set;
        }

        public ITaskHost HostObject
        {
            get;
            set;
        }

        public abstract bool Execute();
    }

    internal class Log : ILog
    {
        private readonly TaskLoggingHelper _logger;
        public Log(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public void LogError(string message, params object[] messageArgs)
        {
            _logger.LogError(message, messageArgs);
        }

        public void LogErrorFromException(Exception exception, bool showStackTrace)
        {
            _logger.LogErrorFromException(exception, showStackTrace);
        }

        public void LogMessage(string message, params object[] messageArgs)
        {
            _logger.LogMessage(message, messageArgs);
        }

        public void LogMessage(LogImportance importance, string message, params object[] messageArgs)
        {
            _logger.LogMessage((MessageImportance)importance, message, messageArgs);
        }

        public void LogWarning(string message, params object[] messageArgs)
        {
            _logger.LogWarning(message, messageArgs);
        }

        public bool HasLoggedErrors { get { return _logger.HasLoggedErrors; } }
    }

    public enum LogImportance
    {
        Low = MessageImportance.Low,
        Normal = MessageImportance.Normal,
        High = MessageImportance.High
    }


    public interface ILog
    {
        //
        // Summary:
        //     Logs an error with the specified message.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     Optional arguments for formatting the message string.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogError(string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a message with the specified string.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     The arguments for formatting the message.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogMessage(string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a message with the specified string and importance.
        //
        // Parameters:
        //   importance:
        //     One of the enumeration values that specifies the importance of the message.
        //
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     The arguments for formatting the message.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogMessage(LogImportance importance, string message, params object[] messageArgs);

        //
        // Summary:
        //     Logs a warning with the specified message.
        //
        // Parameters:
        //   message:
        //     The message.
        //
        //   messageArgs:
        //     Optional arguments for formatting the message string.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     message is null.
        void LogWarning(string message, params object[] messageArgs);
    }
}
