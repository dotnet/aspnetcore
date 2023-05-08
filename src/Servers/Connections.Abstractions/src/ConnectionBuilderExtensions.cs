// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// <see cref="IConnectionBuilder"/> extensions.
/// </summary>
public static class ConnectionBuilderExtensions
{
    /// <summary>
    /// Use the given <typeparamref name="TConnectionHandler"/> <see cref="ConnectionHandler"/>.
    /// </summary>
    /// <typeparam name="TConnectionHandler">The <see cref="Type"/> of the <see cref="ConnectionHandler"/>.</typeparam>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/>.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public static IConnectionBuilder UseConnectionHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConnectionHandler>(this IConnectionBuilder connectionBuilder) where TConnectionHandler : ConnectionHandler
    {
        var handler = ActivatorUtilities.GetServiceOrCreateInstance<TConnectionHandler>(connectionBuilder.ApplicationServices);

        // This is a terminal middleware, so there's no need to use the 'next' parameter
        return connectionBuilder.Run(handler.OnConnectedAsync);
    }

    /// <summary>
    /// Add the given <paramref name="middleware"/> to the connection.
    /// If you aren't calling the next function, use <see cref="Run(IConnectionBuilder, Func{ConnectionContext, Task})"/> instead.
    /// <para>
    /// Prefer using <see cref="Use(IConnectionBuilder, Func{ConnectionContext, ConnectionDelegate, Task})"/> for better performance as shown below:
    /// <code>
    /// builder.Use((context, next) =>
    /// {
    ///     return next(context);
    /// });
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/>.</param>
    /// <param name="middleware">The middleware to add to the <see cref="IConnectionBuilder"/>.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public static IConnectionBuilder Use(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, Func<Task>, Task> middleware)
    {
        return connectionBuilder.Use(next =>
        {
            return context =>
            {
                Func<Task> simpleNext = () => next(context);
                return middleware(context, simpleNext);
            };
        });
    }

    /// <summary>
    /// Add the given <paramref name="middleware"/> to the connection.
    /// If you aren't calling the next function, use <see cref="Run(IConnectionBuilder, Func{ConnectionContext, Task})"/> instead.
    /// </summary>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/>.</param>
    /// <param name="middleware">The middleware to add to the <see cref="IConnectionBuilder"/>.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public static IConnectionBuilder Use(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, ConnectionDelegate, Task> middleware)
    {
        return connectionBuilder.Use(next => context => middleware(context, next));
    }

    /// <summary>
    /// Add the given <paramref name="middleware"/> to the connection.
    /// </summary>
    /// <param name="connectionBuilder">The <see cref="IConnectionBuilder"/>.</param>
    /// <param name="middleware">The middleware to add to the <see cref="IConnectionBuilder"/>.</param>
    /// <returns>The <see cref="IConnectionBuilder"/>.</returns>
    public static IConnectionBuilder Run(this IConnectionBuilder connectionBuilder, Func<ConnectionContext, Task> middleware)
    {
        return connectionBuilder.Use(next =>
        {
            return context =>
            {
                return middleware(context);
            };
        });
    }
}
