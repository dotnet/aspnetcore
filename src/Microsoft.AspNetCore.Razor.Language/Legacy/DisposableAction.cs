// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class DisposableAction : IDisposable
    {
        private readonly Action _action;
        private bool _invoked;

        public DisposableAction(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
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
