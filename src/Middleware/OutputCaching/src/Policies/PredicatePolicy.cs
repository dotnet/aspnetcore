// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A policy that adds a requirement to another policy.
/// </summary>
internal sealed class PredicatePolicy : IOutputCachingPolicy
{
    // TODO: Accept a non async predicate too?

    private readonly Func<OutputCachingContext, Task<bool>> _predicate;
    private readonly IOutputCachingPolicy _policy;

    /// <summary>
    /// Creates a new <see cref="PredicatePolicy"/> instance.
    /// </summary>
    /// <param name="predicate">The predicate.</param>
    /// <param name="policy">The policy.</param>
    public PredicatePolicy(Func<OutputCachingContext, Task<bool>> predicate, IOutputCachingPolicy policy)
    {
        _predicate = predicate;
        _policy = policy;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(OutputCachingContext context)
    {
        return ExecuteAwaited(static (policy, context) => policy.OnRequestAsync(context), _policy, context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(OutputCachingContext context)
    {
        return ExecuteAwaited(static (policy, context) => policy.OnServeFromCacheAsync(context), _policy, context);
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeResponseAsync(OutputCachingContext context)
    {
        return ExecuteAwaited(static (policy, context) => policy.OnServeResponseAsync(context), _policy, context);
    }

    private Task ExecuteAwaited(Func<IOutputCachingPolicy, OutputCachingContext, Task> action, IOutputCachingPolicy policy, OutputCachingContext context)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_predicate == null)
        {
            return action(policy, context);
        }

        var task = _predicate(context);

        if (task.IsCompletedSuccessfully)
        {
            if (task.Result)
            {
                return action(policy, context);
            }

            return Task.CompletedTask;
        }

        return Awaited(task);

        async Task Awaited(Task<bool> task)
        {
            if (await task)
            {
                await action(policy, context);
            }
        }
    }
}
