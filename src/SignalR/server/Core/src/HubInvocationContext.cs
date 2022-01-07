// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// Context for a Hub invocation.
/// </summary>
public class HubInvocationContext
{
    internal ObjectMethodExecutor ObjectMethodExecutor { get; } = default!;

    /// <summary>
    /// Instantiates a new instance of the <see cref="HubInvocationContext"/> class.
    /// </summary>
    /// <param name="context">Context for the active Hub connection and caller.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.</param>
    /// <param name="hub">The instance of the Hub.</param>
    /// <param name="hubMethod">The <see cref="MethodInfo"/> for the Hub method being invoked.</param>
    /// <param name="hubMethodArguments">The arguments provided by the client.</param>
    public HubInvocationContext(HubCallerContext context, IServiceProvider serviceProvider, Hub hub, MethodInfo hubMethod, IReadOnlyList<object?> hubMethodArguments)
    {
        Hub = hub;
        ServiceProvider = serviceProvider;
        HubMethod = hubMethod;
        HubMethodArguments = hubMethodArguments;
        Context = context;
    }

    internal HubInvocationContext(ObjectMethodExecutor objectMethodExecutor, HubCallerContext context, IServiceProvider serviceProvider, Hub hub, object?[] hubMethodArguments)
        : this(context, serviceProvider, hub, objectMethodExecutor.MethodInfo, hubMethodArguments)
    {
        ObjectMethodExecutor = objectMethodExecutor;
    }

    /// <summary>
    /// Gets the context for the active Hub connection and caller.
    /// </summary>
    public HubCallerContext Context { get; }

    /// <summary>
    /// Gets the Hub instance.
    /// </summary>
    public Hub Hub { get; }

    /// <summary>
    /// Gets the name of the Hub method being invoked.
    /// </summary>
    public string HubMethodName => HubMethod.Name;

    /// <summary>
    /// Gets the arguments provided by the client.
    /// </summary>
    public IReadOnlyList<object?> HubMethodArguments { get; }

    /// <summary>
    /// The <see cref="IServiceProvider"/> specific to the scope of this Hub method invocation.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// The <see cref="MethodInfo"/> for the Hub method being invoked.
    /// </summary>
    public MethodInfo HubMethod { get; }
}
