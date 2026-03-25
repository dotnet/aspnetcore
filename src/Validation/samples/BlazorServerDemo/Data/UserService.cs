// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorServerDemo.Data;

/// <summary>
/// Simulates a user repository backed by a database.
/// In a real application, this would query a database or external service.
/// </summary>
public sealed class UserService
{
    private static readonly HashSet<string> RegisteredEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com",
        "test@example.com",
        "user@example.com",
    };

    private static readonly HashSet<string> RegisteredUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "root",
        "blazor",
        "danroth",
    };

    /// <summary>
    /// Checks whether an email address is already registered.
    /// Simulates a 1.5-second database round-trip.
    /// </summary>
    public async Task<bool> IsEmailTakenAsync(string email, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1500, cancellationToken);
        return RegisteredEmails.Contains(email);
    }

    /// <summary>
    /// Checks whether a username is already taken.
    /// Simulates a 2-second database round-trip with occasional infrastructure failures.
    /// </summary>
    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        await Task.Delay(2000, cancellationToken);

        // Simulate a random infrastructure failure ~20% of the time for "blaz" prefix
        if (username.StartsWith("blaz", StringComparison.OrdinalIgnoreCase) && Random.Shared.Next(5) == 0)
        {
            throw new InvalidOperationException("Simulated server error checking username availability.");
        }

        return RegisteredUsernames.Contains(username);
    }
}
