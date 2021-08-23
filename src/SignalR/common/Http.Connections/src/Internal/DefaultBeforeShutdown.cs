// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal class DefaultBeforeShutdown : IBeforeShutdown
    {
        internal List<Func<Task>> Callbacks { get; } = new();

        public IDisposable Register(Func<Task> func)
        {
            Callbacks.Add(func);
            return new Disposable(Callbacks, func);
        }

        private class Disposable : IDisposable
        {
            private readonly List<Func<Task>> _list;
            private readonly Func<Task> _func;

            public Disposable(List<Func<Task>> list, Func<Task> func)
            {
                _list = list;
                _func = func;
            }

            public void Dispose()
            {
                _list.Remove(_func);
            }
        }
    }
}
