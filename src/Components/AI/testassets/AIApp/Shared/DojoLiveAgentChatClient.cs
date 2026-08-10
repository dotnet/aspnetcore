// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AIApp.Components.Scenarios.AgenticGenerativeUI;
using AIApp.Components.Scenarios.PredictiveStateUpdates;
using AIApp.Components.Scenarios.SharedState;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIApp.Shared;

internal interface IDojoLiveAgentDelay
{
    Task DelayAsync(CancellationToken cancellationToken);
}

internal sealed class DojoLiveAgentDelay(TimeSpan delay) : IDojoLiveAgentDelay
{
    public Task DelayAsync(CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}

internal sealed class DojoLiveAgentChatClient : IChatClient
{
    private readonly IChatClient _modelClient;
    private readonly IReadOnlyList<DojoLiveScenarioHandler> _handlers;
    private readonly ILogger _logger;

    public DojoLiveAgentChatClient(
        IChatClient modelClient,
        IDojoLiveAgentDelay delay,
        ILogger<DojoLiveAgentChatClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(modelClient);
        ArgumentNullException.ThrowIfNull(delay);

        _modelClient = modelClient;
        _logger = logger is null ? NullLogger.Instance : logger;
        _handlers =
        [
            new AgenticGenerativeUILiveScenarioHandler(modelClient),
            new SharedStateLiveScenarioHandler(modelClient),
            new PredictiveStateUpdatesLiveScenarioHandler(modelClient, delay),
            new HumanInTheLoopLiveScenarioHandler(modelClient),
            new PassThroughLiveScenarioHandler(
                modelClient,
                new HashSet<string>(["generate_haiku"], StringComparer.Ordinal),
                DojoLivePrompts.ToolBasedGenerativeUI),
            new PassThroughLiveScenarioHandler(
                modelClient,
                new HashSet<string>(["get_weather"], StringComparer.Ordinal),
                DojoLivePrompts.BackendToolRendering),
            new PassThroughLiveScenarioHandler(
                modelClient,
                new HashSet<string>(["change_background"], StringComparer.Ordinal),
                DojoLivePrompts.AgenticChat),
        ];
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var toolNames = DojoLiveScenarioHandler.GetToolNames(options);
        var handler = _handlers.SingleOrDefault(handler => handler.Matches(toolNames));
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"The dojo live agent does not recognize tools [{string.Join(", ", toolNames)}].");
        }

        return GetStreamingResponseCoreAsync(handler, messageList, options, cancellationToken);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseCoreAsync(
        DojoLiveScenarioHandler handler,
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var enumerator = handler.GetStreamingResponseAsync(
            messages,
            options,
            cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            ChatResponseUpdate update;
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                update = enumerator.Current;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailure(handler, exception);
                throw;
            }

            yield return update;
        }
    }

    private void LogFailure(DojoLiveScenarioHandler handler, Exception exception)
    {
        var status = exception.GetType().GetProperty("Status")?.GetValue(exception);
        var errorCode = exception.GetType().GetProperty("ErrorCode")?.GetValue(exception);
        _logger.LogError(
            "Dojo live-agent request failed. Handler: {Handler}; exception type: {ExceptionType}; " +
            "status: {Status}; error code: {ErrorCode}; inner exception type: {InnerExceptionType}; " +
            "stack: {StackTrace}",
            handler.GetType().Name,
            exception.GetType().FullName,
            status ?? "unavailable",
            errorCode ?? "unavailable",
            exception.InnerException?.GetType().FullName ?? "none",
            exception.StackTrace ?? "unavailable");
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetStreamingResponseAsync(messages, options, cancellationToken)
            .ToChatResponseAsync(cancellationToken);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient)
            ? this
            : _modelClient.GetService(serviceType, serviceKey);

    public void Dispose()
    {
        _modelClient.Dispose();
    }
}

internal abstract class DojoLiveScenarioHandler(IChatClient modelClient)
{
    protected static JsonSerializerOptions JsonOptions { get; } =
        new(AIJsonUtilities.DefaultOptions);

    protected IChatClient ModelClient { get; } = modelClient;

    public abstract bool Matches(IReadOnlySet<string> toolNames);

    public abstract IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken);

    internal static IReadOnlySet<string> GetToolNames(ChatOptions? options)
        => options?.Tools?
            .Select(tool => tool.Name)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);

    protected static IReadOnlyList<ChatMessage> AddSystemPrompt(
        IReadOnlyList<ChatMessage> messages,
        string prompt)
        => [new ChatMessage(ChatRole.System, prompt), .. messages];

    protected static ChatOptions? PrepareModelOptions(ChatOptions? options)
    {
        var modelOptions = options?.Clone();
        if (modelOptions is not null)
        {
            modelOptions.RawRepresentationFactory = null;
        }

        return modelOptions;
    }

    protected static ToolExchange? FindLatestToolExchange(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlySet<string> names,
        IReadOnlySet<string>? processedCallIds = null)
    {
        var lastUserMessageIndex = -1;
        for (var index = messages.Count - 1; index >= 0; index--)
        {
            if (messages[index].Role == ChatRole.User)
            {
                lastUserMessageIndex = index;
                break;
            }
        }

        if (lastUserMessageIndex < 0)
        {
            return null;
        }

        var calls = new Dictionary<string, FunctionCallContent>(StringComparer.Ordinal);
        ToolExchange? latest = null;
        for (var index = lastUserMessageIndex + 1; index < messages.Count; index++)
        {
            foreach (var content in messages[index].Contents)
            {
                if (content is FunctionCallContent call && names.Contains(call.Name))
                {
                    calls[call.CallId] = call;
                }
                else if (content is FunctionResultContent result &&
                    calls.TryGetValue(result.CallId, out var matchingCall) &&
                    processedCallIds?.Contains(result.CallId) != true)
                {
                    latest = new ToolExchange(matchingCall, result);
                }
            }
        }

        return latest;
    }

    protected static T GetRequiredArgument<T>(FunctionCallContent call, string name)
    {
        if (call.Arguments?.TryGetValue(name, out var value) != true || value is null)
        {
            throw new InvalidOperationException(
                $"The {call.Name} tool call did not include its required {name} argument.");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        if (value is JsonElement element)
        {
            return element.Deserialize<T>(JsonOptions)
                ?? throw new InvalidOperationException(
                    $"The {call.Name} tool call contained an invalid {name} argument.");
        }

        return JsonSerializer.SerializeToElement(value, JsonOptions).Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException(
                $"The {call.Name} tool call contained an invalid {name} argument.");
    }

    protected static ChatResponseUpdate CreateRawEventUpdate(DojoProtocolEvent protocolEvent)
        => new()
        {
            Role = ChatRole.Assistant,
            RawRepresentation = protocolEvent,
        };

    protected static ChatResponseUpdate NormalizeModelUpdate(ChatResponseUpdate update)
    {
        update.ModelId = null;
        update.RawRepresentation = null;
        update.AdditionalProperties = null;
        foreach (var content in update.Contents)
        {
            content.RawRepresentation = null;
            content.AdditionalProperties = null;
        }

        return update;
    }

    protected async Task<IReadOnlyList<ChatResponseUpdate>> GetBufferedModelUpdatesAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var streamedUpdates = new List<ChatResponseUpdate>();
        await foreach (var update in ModelClient.GetStreamingResponseAsync(
            messages,
            options,
            cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            streamedUpdates.Add(update);
        }

        var response = streamedUpdates.ToChatResponse();
        var bufferedUpdates = new List<ChatResponseUpdate>(response.Messages.Count);
        for (var index = 0; index < response.Messages.Count; index++)
        {
            var message = response.Messages[index];
            bufferedUpdates.Add(NormalizeModelUpdate(new ChatResponseUpdate
            {
                Role = message.Role,
                Contents = [.. message.Contents],
                FinishReason = index == response.Messages.Count - 1
                    ? response.FinishReason
                    : null,
            }));
        }

        return bufferedUpdates;
    }

    protected static void EnsureTextOnlyResponse(
        IReadOnlyList<ChatResponseUpdate> updates,
        string operation)
    {
        if (updates.SelectMany(update => update.Contents).OfType<FunctionCallContent>().Any())
        {
            throw new InvalidOperationException(
                $"The model called a tool while generating the {operation} response.");
        }

        if (!updates.SelectMany(update => update.Contents)
            .OfType<TextContent>()
            .Any(content => !string.IsNullOrWhiteSpace(content.Text)))
        {
            throw new InvalidOperationException(
                $"The model did not generate text for the {operation} response.");
        }
    }

    protected sealed record ToolExchange(
        FunctionCallContent Call,
        FunctionResultContent Result);
}

internal sealed class PassThroughLiveScenarioHandler(
    IChatClient modelClient,
    IReadOnlySet<string> requiredTools,
    string systemPrompt)
    : DojoLiveScenarioHandler(modelClient)
{
    public override bool Matches(IReadOnlySet<string> toolNames)
        => requiredTools.SetEquals(toolNames);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var updates = await GetBufferedModelUpdatesAsync(
            AddSystemPrompt(messages, systemPrompt),
            PrepareModelOptions(options),
            cancellationToken);
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }
}

internal sealed class HumanInTheLoopLiveScenarioHandler(IChatClient modelClient)
    : DojoLiveScenarioHandler(modelClient)
{
    private static readonly HashSet<string> _tools = ["generate_task_steps"];

    public override bool Matches(IReadOnlySet<string> toolNames)
        => _tools.SetEquals(toolNames);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var exchange = FindLatestToolExchange(messages, _tools);
        if (exchange is not null)
        {
            const string selectedPrefix = "The user selected the following steps:";
            var result = exchange.Result.Result?.ToString() ?? "";
            var response = result.StartsWith(selectedPrefix, StringComparison.Ordinal)
                ? $"I'll move forward with the selected tasks: {result[selectedPrefix.Length..].Trim()}."
                : result.Contains("rejected all proposed steps", StringComparison.OrdinalIgnoreCase)
                    ? "No tasks were selected, so I won't move forward with any proposed steps."
                    : throw new InvalidOperationException(
                        "The generate_task_steps tool returned an unsupported result.");
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, response);
            yield break;
        }

        var updates = await GetBufferedModelUpdatesAsync(
            AddSystemPrompt(messages, DojoLivePrompts.HumanInTheLoop),
            PrepareModelOptions(options),
            cancellationToken);
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }
}

internal sealed class AgenticGenerativeUILiveScenarioHandler(IChatClient modelClient)
    : DojoLiveScenarioHandler(modelClient)
{
    private const int MaxPlanSteps = 20;
    private static readonly HashSet<string> _tools =
        ["create_plan", "update_plan_step"];

    private readonly HashSet<string> _processedCallIds = new(StringComparer.Ordinal);
    private PlanState _plan = new();

    public override bool Matches(IReadOnlySet<string> toolNames)
        => _tools.SetEquals(toolNames);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (messages.LastOrDefault()?.Role == ChatRole.User &&
            _plan.Steps.Count > 0)
        {
            _plan = new PlanState();
            _processedCallIds.Clear();
        }

        var exchange = FindLatestToolExchange(messages, _tools, _processedCallIds);
        if (exchange is not null)
        {
            if (exchange.Call.Name == "create_plan")
            {
                var steps = GetRequiredArgument<List<string>>(exchange.Call, "steps");
                if (steps.Count is < 1 or > MaxPlanSteps)
                {
                    throw new InvalidOperationException(
                        $"The create_plan tool must contain between 1 and {MaxPlanSteps} steps.");
                }

                var nextPlan = new PlanState
                {
                    Steps =
                    [
                        .. steps.Select(description => new PlanStep
                        {
                            Description = description,
                            Status = "pending",
                        }),
                    ],
                };

                cancellationToken.ThrowIfCancellationRequested();
                _processedCallIds.Add(exchange.Call.CallId);
                _plan = nextPlan;
                yield return CreateRawEventUpdate(new DojoStateSnapshotEvent
                {
                    Snapshot = JsonSerializer.SerializeToElement(_plan, JsonOptions),
                });
            }
            else
            {
                var index = GetRequiredArgument<int>(exchange.Call, "index");
                if (index < 0 || index >= _plan.Steps.Count)
                {
                    throw new InvalidOperationException(
                        $"The update_plan_step tool call specified invalid step index {index}.");
                }

                var nextPlan = ClonePlan(_plan);
                var operations = new List<object>();
                if (exchange.Call.Arguments?.TryGetValue("description", out var descriptionValue) == true &&
                    descriptionValue is not null)
                {
                    var description = GetRequiredArgument<string>(exchange.Call, "description");
                    nextPlan.Steps[index].Description = description;
                    operations.Add(new
                    {
                        op = "replace",
                        path = $"/steps/{index}/description",
                        value = description,
                    });
                }

                if (exchange.Call.Arguments?.TryGetValue("status", out var statusValue) == true &&
                    statusValue is not null)
                {
                    var status = GetRequiredArgument<string>(exchange.Call, "status").ToLowerInvariant();
                    nextPlan.Steps[index].Status = status;
                    operations.Add(new
                    {
                        op = "replace",
                        path = $"/steps/{index}/status",
                        value = status,
                    });
                }

                cancellationToken.ThrowIfCancellationRequested();
                _processedCallIds.Add(exchange.Call.CallId);
                _plan = nextPlan;
                if (operations.Count > 0)
                {
                    yield return CreateRawEventUpdate(new DojoStateDeltaEvent
                    {
                        Delta = JsonSerializer.SerializeToElement(operations, JsonOptions),
                    });
                }
            }
        }

        var modelOptions = PrepareModelOptions(options);
        if (modelOptions is not null)
        {
            modelOptions.AllowMultipleToolCalls = false;
        }

        var modelUpdates = await GetBufferedModelUpdatesAsync(
            AddSystemPrompt(messages, DojoLivePrompts.AgenticGenerativeUI),
            modelOptions,
            cancellationToken);

        var response = modelUpdates.ToChatResponse();
        var modelToolCall = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .LastOrDefault(call => _tools.Contains(call.Name));
        var nextPendingIndex = _plan.Steps.FindIndex(step => step.Status != "completed");
        if (_plan.Steps.Count > 0 &&
            nextPendingIndex >= 0 &&
            !IsCompletingStep(modelToolCall, nextPendingIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                MessageId = Guid.NewGuid().ToString("N"),
                Contents =
                [
                    new FunctionCallContent(
                        Guid.NewGuid().ToString("N"),
                        "update_plan_step",
                        new Dictionary<string, object?>
                        {
                            ["index"] = nextPendingIndex,
                            ["status"] = "completed",
                        }),
                ],
                FinishReason = ChatFinishReason.ToolCalls,
            };
            yield break;
        }

        if (_plan.Steps.Count > 0 &&
            nextPendingIndex < 0 &&
            modelToolCall is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(
                ChatRole.Assistant,
                $"All {_plan.Steps.Count} plan steps are complete.");
            yield break;
        }

        foreach (var update in modelUpdates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }

    private static PlanState ClonePlan(PlanState plan)
        => new()
        {
            Steps =
            [
                .. plan.Steps.Select(step => new PlanStep
                {
                    Description = step.Description,
                    Status = step.Status,
                }),
            ],
        };

    private static bool IsCompletingStep(FunctionCallContent? call, int expectedIndex)
    {
        if (call?.Name != "update_plan_step" ||
            call.Arguments?.TryGetValue("index", out var indexValue) != true ||
            call.Arguments.TryGetValue("status", out var statusValue) != true)
        {
            return false;
        }

        var index = indexValue switch
        {
            int directIndex => directIndex,
            JsonElement { ValueKind: JsonValueKind.Number } indexElement
                when indexElement.TryGetInt32(out var parsedIndex) => parsedIndex,
            _ => -1,
        };
        var status = statusValue switch
        {
            string directStatus => directStatus,
            JsonElement { ValueKind: JsonValueKind.String } statusElement =>
                statusElement.GetString(),
            _ => null,
        };

        return index == expectedIndex &&
            string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class SharedStateLiveScenarioHandler(IChatClient modelClient)
    : DojoLiveScenarioHandler(modelClient)
{
    private static readonly HashSet<string> _tools = ["generate_recipe"];
    private readonly HashSet<string> _processedCallIds = new(StringComparer.Ordinal);

    public override bool Matches(IReadOnlySet<string> toolNames)
        => _tools.SetEquals(toolNames);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var exchange = FindLatestToolExchange(messages, _tools, _processedCallIds);
        if (exchange is not null)
        {
            var recipe = GetRequiredArgument<Recipe>(exchange.Call, "recipe");
            NormalizeIngredientIcons(recipe);
            var summaryOptions = PrepareModelOptions(options);
            if (summaryOptions is not null)
            {
                summaryOptions.Tools = [];
                summaryOptions.ToolMode = ChatToolMode.None;
            }

            var summaryUpdates = await GetBufferedModelUpdatesAsync(
                AddSystemPrompt(
                    messages,
                    "The recipe tool has completed. Do not call any tools. " +
                    "Provide a concise summary of the recipe state changes in at most two sentences."),
                summaryOptions,
                cancellationToken);
            EnsureTextOnlyResponse(summaryUpdates, "Shared State summary");

            cancellationToken.ThrowIfCancellationRequested();
            _processedCallIds.Add(exchange.Call.CallId);
            yield return CreateRawEventUpdate(new DojoStateSnapshotEvent
            {
                Snapshot = JsonSerializer.SerializeToElement(
                    new RecipeState { Recipe = recipe },
                    JsonOptions),
            });

            foreach (var update in summaryUpdates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }

            yield break;
        }

        var state = options?.RawRepresentationFactory?.Invoke(ModelClient) as RecipeState
            ?? new RecipeState();
        var prompt = $"{DojoLivePrompts.SharedState}\n\n" +
            "Here is the current shared recipe state in JSON format:\n" +
            JsonSerializer.Serialize(state, JsonOptions);

        var updates = await GetBufferedModelUpdatesAsync(
            AddSystemPrompt(messages, prompt),
            PrepareModelOptions(options),
            cancellationToken);
        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }
    }

    private static void NormalizeIngredientIcons(Recipe recipe)
    {
        for (var index = 0; index < recipe.Ingredients.Count; index++)
        {
            var ingredient = recipe.Ingredients[index];
            var icon = NormalizeEscapedIcon(ingredient.Icon);
            if (!IsSingleUnicodeGrapheme(icon))
            {
                throw new InvalidOperationException(
                    $"The generate_recipe tool returned an invalid icon for ingredient {index + 1}. " +
                    "Icons must contain one actual Unicode grapheme.");
            }

            ingredient.Icon = icon;
        }
    }

    private static string NormalizeEscapedIcon(string icon)
    {
        if (TryParseScalarEscape(icon, @"\x", out var scalar) ||
            TryParseScalarEscape(icon, "U+", out scalar))
        {
            return scalar.ToString();
        }

        if (icon.Length >= 6 && icon.Length % 6 == 0)
        {
            var builder = new StringBuilder(icon.Length / 6);
            for (var offset = 0; offset < icon.Length; offset += 6)
            {
                if (!icon.AsSpan(offset, 2).SequenceEqual(@"\u") ||
                    !ushort.TryParse(
                        icon.AsSpan(offset + 2, 4),
                        NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture,
                        out var codeUnit))
                {
                    return icon;
                }

                builder.Append((char)codeUnit);
            }

            return builder.ToString();
        }

        return icon;
    }

    private static bool TryParseScalarEscape(string value, string prefix, out Rune scalar)
    {
        scalar = default;
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            value.Length is >= 4 and <= 8 &&
            int.TryParse(
                value.AsSpan(prefix.Length),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var scalarValue) &&
            Rune.TryCreate(scalarValue, out scalar);
    }

    private static bool IsSingleUnicodeGrapheme(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.EnumerateRunes().Any(rune => rune == Rune.ReplacementChar) ||
            value.EnumerateRunes().All(rune => rune.IsAscii))
        {
            return false;
        }

        var elements = StringInfo.GetTextElementEnumerator(value);
        return elements.MoveNext() && !elements.MoveNext();
    }
}

internal sealed class PredictiveStateUpdatesLiveScenarioHandler(
    IChatClient modelClient,
    IDojoLiveAgentDelay delay)
    : DojoLiveScenarioHandler(modelClient)
{
    private static readonly HashSet<string> _tools =
        ["confirm_changes", "write_document_local"];

    private PendingDocument? _pendingDocument;
    private string? _pendingConfirmationResult;

    public override bool Matches(IReadOnlySet<string> toolNames)
        => _tools.SetEquals(toolNames);

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IReadOnlyList<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var confirmation = FindLatestToolExchange(
            messages,
            new HashSet<string>(["confirm_changes"], StringComparer.Ordinal));
        if (confirmation is not null)
        {
            _pendingConfirmationResult =
                confirmation.Result.Result?.ToString() ?? "The user reviewed the document changes.";
        }

        var isPendingRequestRetry = _pendingDocument is not null &&
            messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text ==
                _pendingDocument.Messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text;
        if (_pendingDocument is not null &&
            _pendingConfirmationResult is not null &&
            (confirmation is not null || isPendingRequestRetry))
        {
            var pendingDocument = _pendingDocument;

            var continuationMessages = new List<ChatMessage>();
            var request = pendingDocument.Messages.LastOrDefault(message => message.Role == ChatRole.User);
            if (request is not null)
            {
                continuationMessages.Add(request);
            }

            continuationMessages.Add(new ChatMessage(
                ChatRole.Assistant,
                [pendingDocument.WriteCall]));
            continuationMessages.Add(new ChatMessage(
                ChatRole.Tool,
                [new FunctionResultContent(
                    pendingDocument.WriteCall.CallId,
                    "Document written.")]));
            continuationMessages.Add(new ChatMessage(
                ChatRole.User,
                _pendingConfirmationResult));

            var summaryOptions = PrepareModelOptions(options);
            if (summaryOptions is not null)
            {
                summaryOptions.Tools = [];
                summaryOptions.ToolMode = ChatToolMode.None;
            }

            var summaryUpdates = await GetBufferedModelUpdatesAsync(
                AddSystemPrompt(
                    continuationMessages,
                    "The document change has already been reviewed. Do not call any tools. " +
                    "Briefly summarize whether the change was confirmed or rejected in at most two sentences."),
                summaryOptions,
                cancellationToken);
            EnsureTextOnlyResponse(summaryUpdates, "Predictive State Updates summary");
            foreach (var update in summaryUpdates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }

            cancellationToken.ThrowIfCancellationRequested();
            _pendingDocument = null;
            _pendingConfirmationResult = null;
            yield break;
        }

        if (_pendingDocument is not null &&
            messages.LastOrDefault()?.Role == ChatRole.User)
        {
            _pendingDocument = null;
            _pendingConfirmationResult = null;
        }

        var state = options?.RawRepresentationFactory?.Invoke(ModelClient) as DocumentState
            ?? new DocumentState();
        var prompt = $"{DojoLivePrompts.PredictiveStateUpdates}\n\n" +
            "Here is the current document state in JSON format:\n" +
            JsonSerializer.Serialize(state, JsonOptions);
        var sanitizedMessages = RemoveConfirmationMessages(messages);
        var modelUpdates = await GetBufferedModelUpdatesAsync(
            AddSystemPrompt(sanitizedMessages, prompt),
            PrepareModelOptions(options),
            cancellationToken);

        var response = modelUpdates.ToChatResponse();
        var writeCall = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .LastOrDefault(call => call.Name == "write_document_local");
        if (writeCall is null)
        {
            foreach (var update in modelUpdates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return update;
            }

            yield break;
        }

        var document = GetRequiredArgument<string>(writeCall, "document");
        _pendingDocument = new PendingDocument(sanitizedMessages, writeCall);
        const int chunkSize = 10;
        for (var length = Math.Min(chunkSize, document.Length);
            length <= document.Length;
            length = Math.Min(length + chunkSize, document.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return CreateRawEventUpdate(new DojoStateSnapshotEvent
            {
                Snapshot = JsonSerializer.SerializeToElement(
                    new DocumentState { Document = document[..length] },
                    JsonOptions),
            });

            if (length == document.Length)
            {
                break;
            }

            await delay.DelayAsync(cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        yield return new ChatResponseUpdate
        {
            Role = ChatRole.Assistant,
            MessageId = Guid.NewGuid().ToString("N"),
            Contents =
            [
                new FunctionCallContent(
                    Guid.NewGuid().ToString("N"),
                    "confirm_changes",
                    arguments: null),
            ],
            FinishReason = ChatFinishReason.ToolCalls,
        };
    }

    private static IReadOnlyList<ChatMessage> RemoveConfirmationMessages(
        IReadOnlyList<ChatMessage> messages)
    {
        var confirmationCallIds = messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .Where(call => call.Name == "confirm_changes")
            .Select(call => call.CallId)
            .ToHashSet(StringComparer.Ordinal);

        if (confirmationCallIds.Count == 0)
        {
            return messages;
        }

        var result = new List<ChatMessage>();
        foreach (var message in messages)
        {
            var contents = message.Contents
                .Where(content => content switch
                {
                    FunctionCallContent call => !confirmationCallIds.Contains(call.CallId),
                    FunctionResultContent functionResult =>
                        !confirmationCallIds.Contains(functionResult.CallId),
                    _ => true,
                })
                .ToList();
            if (contents.Count > 0)
            {
                result.Add(new ChatMessage(message.Role, contents));
            }
        }

        return result;
    }

    private sealed record PendingDocument(
        IReadOnlyList<ChatMessage> Messages,
        FunctionCallContent WriteCall);
}

internal static class DojoLivePrompts
{
    public const string AgenticChat = """
        You are a helpful chat assistant. The user's name is Bob.
        Use change_background only when the user asks to change the background.
        When changing it, prefer an attractive CSS gradient and briefly acknowledge the result after the tool completes.
        """;

    public const string BackendToolRendering = """
        You are a helpful weather assistant.
        For any request about weather, call get_weather with the requested location.
        After the tool returns, summarize the conditions concisely.
        """;

    public const string HumanInTheLoop = """
        You are a planning assistant.
        When asked to create a plan, call generate_task_steps so the user can review the steps.
        A request for a simple plan must contain exactly 5 sensible steps.
        A request for a complex plan must contain exactly 10 sensible steps.
        Keep all supported plans between 5 and 10 steps and set every initial status to "enabled".
        After the tool returns, mention exactly the selected steps and do not mention disabled steps as selected.
        If the user rejected every step, acknowledge that no steps will be performed.
        """;

    public const string ToolBasedGenerativeUI = """
        You are a Japanese haiku assistant.
        For every haiku request, call generate_haiku with exactly three Japanese lines, exactly three English
        translation lines, a relevant image_name, and an attractive CSS gradient.
        Do not print the haiku as ordinary chat text before calling the tool.
        """;

    public const string AgenticGenerativeUI = """
        When planning use tools only, without any other messages.
        IMPORTANT:
        - Use the `create_plan` tool to set the initial state of the steps
        - Use the `update_plan_step` tool to update the status of each step
        - Do NOT repeat the plan or summarise it in a message
        - Do NOT confirm the creation or individual updates in a message
        - Do NOT ask the user for additional information or next steps
        - Do NOT leave a plan hanging, always complete the plan via `update_plan_step` if one is ongoing.
        - Continue calling update_plan_step until all steps are marked as completed.
        - After all steps are completed, provide one brief acknowledgement.

        Only one plan can be active at a time, so do not call the `create_plan` tool
        again until all the steps in current plan are completed.
        """;

    public const string SharedState = """
        You are a helpful recipe assistant that maintains a shared recipe state with the user.

        IMPORTANT:
        - When the user asks you to create, change, or improve a recipe, call the
          `generate_recipe` tool with a COMPLETE recipe: a title, skill_level, cooking_time,
          special_preferences, the full list of ingredients (each with an icon, name and
          amount) and the step-by-step instructions.
        - Each ingredient icon must be one actual Unicode emoji grapheme. Never return an
          escaped code point string such as \x1f345, \uD83C\uDF45, or U+1F345.
        - Always include every ingredient the recipe needs, keeping any the user already added.
        - When the user only asks a question about the recipe, answer in plain text and do
          NOT call the tool.
        - After the tool result, provide a concise summary of the state changes in at most two sentences.
        """;

    public const string PredictiveStateUpdates = """
        You are a document editor assistant. When asked to write or edit content:

        IMPORTANT:
        - Use the `write_document_local` tool with the full document text in Markdown format
        - Format the document extensively so it's easy to read
        - You can use all kinds of markdown (headings, lists, bold, etc.)
        - However, do NOT use italic or strike-through formatting
        - You MUST write the full document, even when changing only a few words
        - When making edits to the document, try to make them minimal - do not change every word
        - Keep stories SHORT!

        After the user confirms or rejects the changes, briefly summarize the outcome in at most two sentences.
        """;
}
