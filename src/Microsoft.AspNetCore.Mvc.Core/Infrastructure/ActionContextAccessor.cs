// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET451
using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class ActionContextAccessor : IActionContextAccessor
    {
#if NET451
        private static readonly string Key = typeof(ActionContext).FullName + AppDomain.CurrentDomain.Id;

        public ActionContext ActionContext
        {
            get
            {
                var handle = CallContext.LogicalGetData(Key) as ObjectHandle;
                return handle != null ? (ActionContext)handle.Unwrap() : null;
            }
            set
            {
                CallContext.LogicalSetData(Key, new ObjectHandle(value));
            }
        }
#else
        private readonly AsyncLocal<ActionContext> _storage = new AsyncLocal<ActionContext>();

        public ActionContext ActionContext
        {
            get { return _storage.Value; }
            set { _storage.Value = value; }
        }
#endif
    }
}