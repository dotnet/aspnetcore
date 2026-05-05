// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorUnitedApp.Services;

public sealed class UserAvailabilityService
{
    private static readonly HashSet<string> _takenUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "root", "guest", "alice", "bob",
    };

    private static readonly HashSet<string> _blockedEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "blocked.example", "tempmail.example",
    };

    public async Task<bool> IsUsernameAvailableAsync(string? username, CancellationToken cancellationToken)
    {
        await Task.Delay(800, cancellationToken);

        if (string.IsNullOrWhiteSpace(username))
        {
            return true;
        }

        if (username.EndsWith("-boom", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated downstream failure during username lookup.");
        }

        return !_takenUsernames.Contains(username);
    }

    public async Task<bool> IsEmailDomainReachableAsync(string? email, CancellationToken cancellationToken)
    {
        await Task.Delay(600, cancellationToken);

        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return true;
        }

        var domain = email[(atIndex + 1)..];
        if (domain.Equals("boom.example", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Simulated downstream failure during email lookup.");
        }

        return !_blockedEmailDomains.Contains(domain);
    }
}
