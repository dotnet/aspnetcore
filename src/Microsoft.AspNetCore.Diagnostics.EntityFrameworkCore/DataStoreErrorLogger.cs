// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Utilities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
#if NETSTANDARD1_3
using System.Threading;
#else
using System.Runtime.Remoting.Messaging;
#endif

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore
{
    public class DataStoreErrorLogger : ILogger
    {
#if NETSTANDARD1_3
        private readonly AsyncLocal<DataStoreErrorLog> _log = new AsyncLocal<DataStoreErrorLog>(); 
#else
        private const string ContextName = "__DataStoreErrorLog";
#endif

        public virtual DataStoreErrorLog LastError
        {
            get
            {
#if NETSTANDARD1_3
                return _log.Value; 
#else
                return (DataStoreErrorLog)CallContext.LogicalGetData(ContextName);
#endif
            }
        }

        public virtual void StartLoggingForCurrentCallContext()
        {
            // Because CallContext is cloned at each async operation we cannot
            // lazily create the error object when an error is encountered, otherwise
            // it will not be available to code outside of the current async context. 
            // We create it ahead of time so that any cloning just clones the reference
            // to the object that will hold any errors.
#if NETSTANDARD1_3
            _log.Value = new DataStoreErrorLog();
#else
            CallContext.LogicalSetData(ContextName, new DataStoreErrorLog());
#endif
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, [CanBeNull] TState state, [CanBeNull] Exception exception, [CanBeNull] Func<TState, Exception, string> formatter)
        {
            var errorState = state as DatabaseErrorLogState;
            if (errorState != null && exception != null && LastError != null)
            {
                LastError.SetError(errorState.ContextType, exception);
            }
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public virtual IDisposable BeginScope<TState>(TState state)
        {
            return NullScope.Instance;
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance = new NullScope();

            public void Dispose()
            { }
        }

        public class DataStoreErrorLog
        {
            private Type _contextType;
            private Exception _exception;

            public virtual void SetError([NotNull] Type contextType, [NotNull] Exception exception)
            {
                Check.NotNull(contextType, nameof(contextType));
                Check.NotNull(exception, nameof(exception));

                _contextType = contextType;
                _exception = exception;
            }

            public virtual bool IsErrorLogged
            {
                get { return _exception != null; }
            }

            public virtual Type ContextType
            {
                get { return _contextType; }
            }

            public virtual Exception Exception
            {
                get { return _exception; }
            }
        }
    }
}
