// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets
{
    public class SocketBuilder : ISocketBuilder
    {
        private readonly IList<Func<SocketDelegate, SocketDelegate>> _components = new List<Func<SocketDelegate, SocketDelegate>>();

        public IServiceProvider ApplicationServices { get; }

        public SocketBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public ISocketBuilder Use(Func<SocketDelegate, SocketDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        public SocketDelegate Build()
        {
            SocketDelegate app = features =>
            {
                return Task.CompletedTask;
            };

            foreach (var component in _components.Reverse())
            {
                app = component(app);
            }

            return app;
        }
    }
}
