// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MvcValidationDemo.Controllers;

/// <summary>
/// Provides remote validation endpoints for the registration form.
/// Uses deterministic mock data with simulated network delay.
/// </summary>
public class ValidationController : Controller
{
    private static readonly HashSet<string> TakenEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin@example.com",
        "test@example.com",
        "user@example.com",
    };

    private static readonly HashSet<string> TakenUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "root",
        "test",
        "user",
    };

    [AcceptVerbs("GET")]
    public async Task<IActionResult> CheckEmail(string email)
    {
        // Simulate network/database delay
        await Task.Delay(800);

        if (TakenEmails.Contains(email))
        {
            return Json($"The email '{email}' is already registered.");
        }

        return Json(true);
    }

    [AcceptVerbs("GET")]
    public async Task<IActionResult> CheckUsername(string username)
    {
        // Simulate network/database delay
        await Task.Delay(500);

        if (TakenUsernames.Contains(username))
        {
            return Json($"The username '{username}' is already taken.");
        }

        return Json(true);
    }
}
