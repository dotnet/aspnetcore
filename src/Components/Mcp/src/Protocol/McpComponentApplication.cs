// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.Mcp.Protocol;

internal sealed class McpComponentApplication
{
    private const int MaximumViews = 1024;
    private readonly Dictionary<Guid, View> _views = new();
    private readonly object _lock = new();

    public McpComponentApplication(
        string name,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        Uri baseUri,
        Uri initialLocation,
        bool preserveDom = true,
        LogLevel logLevel = LogLevel.Information,
        string? openingToolName = null,
        Func<CallToolResult, IReadOnlyDictionary<string, object?>>? initialParametersFactory = null)
    {
        Name = name;
        ComponentType = componentType;
        BaseUri = baseUri;
        InitialLocation = initialLocation;
        ResourceUri = $"ui://blazor/{Uri.EscapeDataString(name)}";
        DispatchToolName = $"{name}_dispatch";
        PreserveDom = preserveDom;
        LogLevel = logLevel;
        OpeningToolName = openingToolName ?? name;
        InitialParametersFactory = initialParametersFactory;
    }

    public string Name { get; }
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type ComponentType { get; }
    public Uri BaseUri { get; }
    public Uri InitialLocation { get; }
    public string ResourceUri { get; }
    public string DispatchToolName { get; }
    public bool PreserveDom { get; }
    public LogLevel LogLevel { get; }
    public string OpeningToolName { get; }
    public Func<CallToolResult, IReadOnlyDictionary<string, object?>>? InitialParametersFactory { get; }

    public async ValueTask<View> CreateViewAsync(ClaimsPrincipal owner, McpComponentParameters parameters)
    {
        View[] expired;
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            expired = _views.Values.Where(view => view.Expires <= now && !view.HasActiveOperations).ToArray();
            foreach (var view in expired)
            {
                _views.Remove(view.Id);
            }
        }

        await Task.WhenAll(expired.Select(view => view.CloseAsync()));
        lock (_lock)
        {
            if (_views.Count >= MaximumViews)
            {
                throw new McpException("The component application has reached its active view limit.");
            }

            var view = new View(GetPrincipalAssociation(owner), parameters);
            _views.Add(view.Id, view);
            return view;
        }
    }

    public View GetView(Guid id, ClaimsPrincipal owner)
    {
        lock (_lock)
        {
            if (!_views.TryGetValue(id, out var view) || view.Owner != GetPrincipalAssociation(owner) || view.Expires <= DateTimeOffset.UtcNow)
            {
                throw new McpException("The component view is not available for this request.");
            }

            return view;
        }
    }

    public async Task RemoveViewAsync(View view)
    {
        lock (_lock)
        {
            _views.Remove(view.Id);
        }

        await view.CloseAsync();
    }

    private static string GetPrincipalAssociation(ClaimsPrincipal principal)
    {
        var identities = principal.Identities.Select(identity => JsonSerializer.Serialize(new
        {
            identity.AuthenticationType,
            identity.IsAuthenticated,
            identity.NameClaimType,
            identity.RoleClaimType,
            Claims = identity.Claims.Select(claim => new { claim.Type, claim.Value, claim.ValueType, claim.Issuer, claim.OriginalIssuer })
                .OrderBy(claim => claim.Type, StringComparer.Ordinal)
                .ThenBy(claim => claim.Value, StringComparer.Ordinal)
                .ThenBy(claim => claim.ValueType, StringComparer.Ordinal)
                .ThenBy(claim => claim.Issuer, StringComparer.Ordinal)
                .ThenBy(claim => claim.OriginalIssuer, StringComparer.Ordinal),
        })).OrderBy(identity => identity, StringComparer.Ordinal).ToArray();
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(identities)));
    }

    internal sealed class View(string owner, McpComponentParameters parameters)
    {
        private readonly object _operationLock = new();
        private readonly HashSet<Task> _activeOperations = new();
        private readonly CancellationTokenSource _lifetime = new();
        private bool _closed;
        private Task? _closeTask;

        public Guid Id { get; } = Guid.NewGuid();
        public string Owner { get; } = owner;
        public McpComponentParameters Parameters { get; } = parameters;
        public DateTimeOffset Expires { get; } = DateTimeOffset.UtcNow.AddMinutes(20);

        public bool HasActiveOperations
        {
            get
            {
                lock (_operationLock)
                {
                    return _activeOperations.Count != 0;
                }
            }
        }

        public async Task<TResult> RunAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationToken lifetime;
            lock (_operationLock)
            {
                if (_closed || Expires <= DateTimeOffset.UtcNow)
                {
                    throw new McpException("The component view has closed or expired.");
                }

                lifetime = _lifetime.Token;
                _activeOperations.Add(finished.Task);
            }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellationToken);
                return await operation(linked.Token);
            }
            finally
            {
                lock (_operationLock)
                {
                    _activeOperations.Remove(finished.Task);
                }

                finished.SetResult();
            }
        }

        public Task CloseAsync()
        {
            lock (_operationLock)
            {
                _closed = true;
                return _closeTask ??= CompleteCloseAsync(_activeOperations.ToArray());
            }
        }

        private async Task CompleteCloseAsync(Task[] pending)
        {
            try
            {
                await _lifetime.CancelAsync();
            }
            finally
            {
                await Task.WhenAll(pending);
                _lifetime.Dispose();
            }
        }
    }
}
