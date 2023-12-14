// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching.Policies;

/// <summary>
/// A policy that adds a requirement to another policy.
/// </summary>
internal sealed class PredicatePolicy : IOutputCachePolicy
{
    // TODO: Accept a non async predicate too?

    private readonly Func<OutputCacheContext, ValueTask<bool>> _predicate;
    private readonly IOutputCachePolicy _policy;

    /// <summary>
    /// Creates a new <see cref="PredicatePolicy"/> instance.
    /// </summary>
    /// <param name="asyncPredicate">The predicate.</param>
    /// <param name="policy">The policy.</param>
    public PredicatePolicy(Func<OutputCacheContext, ValueTask<bool>> asyncPredicate, IOutputCachePolicy policy)
    {
        _predicate = asyncPredicate;
        _policy = policy;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ExecuteAwaited(static (policy, context, cancellationToken) => policy.CacheRequestAsync(context, cancellationToken), _policy, context, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ExecuteAwaited(static (policy, context, cancellationToken) => policy.ServeFromCacheAsync(context, cancellationToken), _policy, context, cancellationToken);
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ExecuteAwaited(static (policy, context, cancellationToken) => policy.ServeResponseAsync(context, cancellationToken), _policy, context, cancellationToken);
    }

    private ValueTask ExecuteAwaited(Func<IOutputCachePolicy, OutputCacheContext, CancellationToken, ValueTask> action, IOutputCachePolicy policy, OutputCacheContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_predicate == null)
        {
            return action(policy, context, cancellationToken);
        }

        var task = _predicate(context);

        if (task.IsCompletedSuccessfully)
        {
            if (task.Result)
            {
                return action(policy, context, cancellationToken);
            }

            return ValueTask.CompletedTask;
        }

        return Awaited(task);

        async ValueTask Awaited(ValueTask<bool> task)
        {
            if (await task)
            {
                await action(policy, context, cancellationToken);
            }
        }
    }
}
