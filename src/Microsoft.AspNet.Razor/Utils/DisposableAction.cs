// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Utils
{
    internal class DisposableAction : IDisposable
    {
        private Action _action;
        private bool _invoked;

        public DisposableAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            _action = action;
        }

        public void Dispose()
        {
            if (!_invoked)
            {
                _action();
                _invoked = true;
            }
        }
    }
}
