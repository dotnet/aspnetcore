// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// A default implementation for <see cref="IMultiplexedConnectionBuilder"/>.
    /// </summary>
    public class MultiplexedConnectionBuilder : IMultiplexedConnectionBuilder
    {
        private readonly IList<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>> _components = new List<Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate>>();

        /// <inheritdoc />
        public IServiceProvider ApplicationServices { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MultiplexedConnectionBuilder"/>.
        /// </summary>
        /// <param name="applicationServices">The application services <see cref="IServiceProvider"/>.</param>
        public MultiplexedConnectionBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        /// <inheritdoc />
        public IMultiplexedConnectionBuilder Use(Func<MultiplexedConnectionDelegate, MultiplexedConnectionDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }

        /// <inheritdoc />
        public MultiplexedConnectionDelegate Build()
        {
            MultiplexedConnectionDelegate app = features =>
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
