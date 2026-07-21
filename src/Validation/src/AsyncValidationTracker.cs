// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Coordinates validations that may run concurrently once they go asynchronous.
/// Reuses a single <see cref="ValidateContext"/> while validations complete synchronously and
/// clones it only after one goes async, so two concurrently-running validations never share a context.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal struct AsyncValidationTracker
{
    private readonly ValidateContext _originalContext;
    private readonly ValidateContextMutableState _originalState;

    private bool _nextNeedsClone;
    private ValidateContext _currentContext;
    private List<ValidateContext>? _clonedContexts;
    private List<Task>? _pendingTasks;

    public AsyncValidationTracker(ValidateContext context)
    {
        _originalContext = context;
        _currentContext = context;
        _originalState = context.CaptureMutableState();
    }

    // Reuses the context while validations complete synchronously; clones only after one goes async,
    // so two concurrently-running validations never share a context.
    public ValidateContext NextContext()
    {
        if (_nextNeedsClone)
        {
            _currentContext = _originalContext.CopyWithState(_originalState);
            (_clonedContexts ??= []).Add(_currentContext);
            _nextNeedsClone = false;
        }

        return _currentContext;
    }

    public void Track(Task validationTask)
    {
        if (validationTask.IsCompletedSuccessfully)
        {
            return; // synchronous: keep using the same context
        }

        _nextNeedsClone = true; // the next item must get its own clone
        (_pendingTasks ??= []).Add(validationTask);
    }

    // Stays fully synchronous when nothing was tracked; otherwise awaits all and merges clone errors back.
    public readonly Task<bool> CompleteAsync()
        => _pendingTasks is null ? Task.FromResult(false) : AwaitAndMergeAsync(_pendingTasks, _clonedContexts, _originalContext);

    private static async Task<bool> AwaitAndMergeAsync(List<Task> pendingTasks, List<ValidateContext>? clonedContexts, ValidateContext originalContext)
    {
        await Task.WhenAll(pendingTasks);
        return originalContext.MergeErrorsFromClonedContexts(clonedContexts);
    }
}
