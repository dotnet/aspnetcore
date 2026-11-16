// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Components.Authorization;

// An authorization service whose evaluations stay pending until the test decides the
// outcome, so overlapping evaluations can be made to complete in any order.
public class GatedTestAuthorizationService : IAuthorizationService
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _gates = new();
    private readonly ConcurrentQueue<string> _startedEvaluations = new();

    public string[] StartedEvaluations => [.. _startedEvaluations];

    public void Grant(string policyName) => GateFor(policyName).TrySetResult(true);

    public void Deny(string policyName) => GateFor(policyName).TrySetResult(false);

    public async Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        var policyName = requirements.OfType<TestPolicyRequirement>().SingleOrDefault()?.PolicyName
            ?? throw new InvalidOperationException(
                $"{nameof(GatedTestAuthorizationService)} tells evaluations apart by policy name, so each one must " +
                $"come from a named policy supplied by {nameof(TestAuthorizationPolicyProvider)}.");

        // Recorded before the first await so that a test can inspect StartedEvaluations as soon as
        // the render which triggered the evaluation has returned.
        _startedEvaluations.Enqueue(policyName);

        return await GateFor(policyName).Task
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed();
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
        => throw new NotSupportedException();

    private TaskCompletionSource<bool> GateFor(string policyName)
        => _gates.GetOrAdd(
            policyName,
            static _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
}
