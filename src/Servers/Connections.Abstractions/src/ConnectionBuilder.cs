// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// A default implementation for <see cref="IConnectionBuilder"/>.
/// </summary>
public class ConnectionBuilder : IConnectionBuilder
{
    private readonly IList<Func<ConnectionDelegate, ConnectionDelegate>> _components = new List<Func<ConnectionDelegate, ConnectionDelegate>>();

    /// <inheritdoc />
    public IServiceProvider ApplicationServices { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ConnectionBuilder"/>.
    /// </summary>
    /// <param name="applicationServices">The application services <see cref="IServiceProvider"/>.</param>
    public ConnectionBuilder(IServiceProvider applicationServices)
    {
        ApplicationServices = applicationServices;
    }

    /// <inheritdoc />
    public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    /// <inheritdoc />
    public ConnectionDelegate Build()
    {
        ConnectionDelegate app = features =>
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
