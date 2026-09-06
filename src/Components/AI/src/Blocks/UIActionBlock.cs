// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace Microsoft.AspNetCore.Components.AI;

/// <summary>
/// Represents a function call that must execute in the UI before the conversation can continue.
/// </summary>
/// <remarks>
/// Register the corresponding function with
/// <see cref="UIAgentOptions.RegisterUIAction(AIFunction)"/>. Renderers can call
/// <see cref="InvokeAsync(CancellationToken)"/> to execute it in the current UI circuit.
/// </remarks>
/// <example>
/// <code>
/// &lt;BlockRenderer TBlock="UIActionBlock" Context="action"&gt;
///     &lt;button @onclick="() =&gt; action.InvokeAsync()"&gt;Run&lt;/button&gt;
/// &lt;/BlockRenderer&gt;
/// </code>
/// </example>
public class UIActionBlock : ContentBlock, IInteractiveBlock
{
    private readonly AIFunction _function;
    private readonly TaskCompletionSource<AIContent> _resultSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _invocationLock = new();
    private Task? _invocation;

    internal UIActionBlock(AIFunction function, FunctionCallContent call)
    {
        _function = function;
        Call = call;
    }

    /// <summary>
    /// Gets the function call requested by the model.
    /// </summary>
    public FunctionCallContent Call { get; }

    /// <summary>
    /// Gets the registered UI action name.
    /// </summary>
    public string ToolName => Call.Name;

    /// <summary>
    /// Gets the function result after the action completes.
    /// </summary>
    public FunctionResultContent? Result { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the action completed successfully.
    /// </summary>
    public bool IsComplete => Result is not null;

    /// <summary>
    /// Executes the registered function once using the arguments supplied by the model.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that cancels the action when this call starts it. Subsequent calls return the
    /// existing invocation task and do not replace its cancellation token.
    /// </param>
    /// <returns>A task that completes when the action has finished.</returns>
    public Task InvokeAsync(CancellationToken cancellationToken = default)
    {
        lock (_invocationLock)
        {
            _invocation ??= InvokeCoreAsync(cancellationToken);
            return _invocation;
        }
    }

    /// <inheritdoc />
    public Task<AIContent> GetResultAsync(CancellationToken cancellationToken = default)
        => _resultSource.Task.WaitAsync(cancellationToken);

    private async Task InvokeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var arguments = Call.Arguments is not null
                ? new AIFunctionArguments(Call.Arguments)
                : null;
            var result = await _function.InvokeAsync(arguments, cancellationToken);
            Result = new FunctionResultContent(Call.CallId ?? Id, result);
            NotifyChanged();
            _resultSource.TrySetResult(Result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _resultSource.TrySetCanceled(cancellationToken);
            throw;
        }
        catch (Exception exception)
        {
            _resultSource.TrySetException(exception);
            throw;
        }
    }
}
