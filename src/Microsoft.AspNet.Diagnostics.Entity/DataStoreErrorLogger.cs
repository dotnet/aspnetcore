// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using Microsoft.Data.Entity.Storage;
using Microsoft.Framework.Logging;
using System;
#if ASPNETCORE50
using System.Threading;
#else
using System.Runtime.Remoting.Messaging;
#endif

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DataStoreErrorLogger : ILogger
    {
#if ASPNETCORE50
        private readonly AsyncLocal<DataStoreErrorLog> _log = new AsyncLocal<DataStoreErrorLog>(); 
#else
        private const string ContextName = "__DataStoreErrorLog";
#endif

        public virtual DataStoreErrorLog LastError
        {
            get
            {
#if ASPNETCORE50
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
#if ASPNETCORE50
            _log.Value = new DataStoreErrorLog();
#else
            CallContext.LogicalSetData(ContextName, new DataStoreErrorLog());
#endif
        }

        public virtual void Write(LogLevel logLevel, int eventId, [CanBeNull] object state, [CanBeNull] Exception exception, [CanBeNull] Func<object, Exception, string> formatter)
        {
            var errorState = state as DataStoreErrorLogState;
            if (errorState != null && exception != null && LastError != null)
            {
                LastError.SetError(errorState.ContextType, exception);
            }
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public virtual IDisposable BeginScope(object state)
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
                Check.NotNull(contextType, "contextType");
                Check.NotNull(exception, "exception");

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