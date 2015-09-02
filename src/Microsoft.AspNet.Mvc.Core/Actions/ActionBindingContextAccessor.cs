// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace Microsoft.AspNet.Mvc.Actions
{
    public class ActionBindingContextAccessor : IActionBindingContextAccessor
    {
#if DNX451
        private static string Key = typeof(ActionBindingContext).FullName;

        public ActionBindingContext ActionBindingContext
        {
            get
            {
                var handle = CallContext.LogicalGetData(Key) as ObjectHandle;
                return handle != null ? (ActionBindingContext)handle.Unwrap() : null;
            }
            set
            {
                CallContext.LogicalSetData(Key, new ObjectHandle(value));
            }
        }
#else
        private readonly AsyncLocal<ActionBindingContext> _storage = new AsyncLocal<ActionBindingContext>();

        public ActionBindingContext ActionBindingContext
        {
            get { return _storage.Value; }
            set { _storage.Value = value; }
        }
#endif
    }
}