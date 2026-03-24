// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorUnitedApp.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BlazorUnitedApp.Shared;

/// <summary>
/// A custom validator component that demonstrates the new async validation APIs.
/// It performs synchronous DataAnnotations validation plus simulated async
/// "uniqueness" checks for the Email and Username fields.
/// </summary>
public class AsyncSimulatedValidator : ComponentBase, IDisposable
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
        "blazor",
    };

    private ValidationMessageStore _messages = default!;

    [CascadingParameter]
    private EditContext EditContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(AsyncSimulatedValidator)} requires a cascading parameter of type {nameof(EditContext)}.");
        }

        _messages = new ValidationMessageStore(EditContext);

        // Subscribe to sync field change for DataAnnotations property validation
        EditContext.OnFieldChanged += OnFieldChanged;

        // Subscribe to async form-submit validation
        EditContext.OnValidationRequestedAsync += OnValidationRequestedAsync;
    }

    /// <summary>
    /// Per-field validation on change: runs sync DataAnnotations first, then kicks off
    /// an async "uniqueness" check and registers it via AddValidationTask for tracking.
    /// </summary>
    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        var fieldIdentifier = e.FieldIdentifier;

        // Run synchronous DataAnnotations validation for this property
        RunSyncPropertyValidation(fieldIdentifier);

        // If the field supports async validation, start it and register for tracking
        if (IsAsyncField(fieldIdentifier))
        {
            var cts = new CancellationTokenSource();
            var task = RunFieldAsyncValidationAsync(fieldIdentifier, cts.Token);
            EditContext.AddValidationTask(fieldIdentifier, task, cts);
        }
    }

    /// <summary>
    /// Full form-submit async validation: runs sync DataAnnotations on the entire model,
    /// then performs all async checks.
    /// </summary>
    private async Task OnValidationRequestedAsync(object sender, ValidationRequestedEventArgs e)
    {
        _messages.Clear();

        if (EditContext.Model is not Registration model)
        {
            return;
        }

        // Run synchronous DataAnnotations on the full object
        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, results, validateAllProperties: true);

        foreach (var result in results)
        {
            foreach (var memberName in result.MemberNames)
            {
                _messages.Add(EditContext.Field(memberName), result.ErrorMessage!);
            }
        }

        // Run async checks for Email and Username in parallel
        var emailTask = ValidateEmailAsync(model.Email, CancellationToken.None);
        var usernameTask = ValidateUsernameAsync(model.Username, CancellationToken.None);

        var emailErrors = await emailTask;
        var usernameErrors = await usernameTask;

        foreach (var error in emailErrors)
        {
            _messages.Add(EditContext.Field(nameof(Registration.Email)), error);
        }

        foreach (var error in usernameErrors)
        {
            _messages.Add(EditContext.Field(nameof(Registration.Username)), error);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private void RunSyncPropertyValidation(in FieldIdentifier fieldIdentifier)
    {
        if (fieldIdentifier.Model is not Registration model)
        {
            return;
        }

        var propertyInfo = model.GetType().GetProperty(fieldIdentifier.FieldName);
        if (propertyInfo is null)
        {
            return;
        }

        var propertyValue = propertyInfo.GetValue(model);
        var validationContext = new ValidationContext(model) { MemberName = propertyInfo.Name };
        var results = new List<ValidationResult>();

        Validator.TryValidateProperty(propertyValue, validationContext, results);

        _messages.Clear(fieldIdentifier);
        foreach (var result in results)
        {
            _messages.Add(fieldIdentifier, result.ErrorMessage!);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private async Task RunFieldAsyncValidationAsync(FieldIdentifier fieldIdentifier, CancellationToken cancellationToken)
    {
        if (fieldIdentifier.Model is not Registration model)
        {
            return;
        }

        var errors = fieldIdentifier.FieldName switch
        {
            nameof(Registration.Email) => await ValidateEmailAsync(model.Email, cancellationToken),
            nameof(Registration.Username) => await ValidateUsernameAsync(model.Username, cancellationToken),
            _ => [],
        };

        foreach (var error in errors)
        {
            _messages.Add(fieldIdentifier, error);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private static async Task<List<string>> ValidateEmailAsync(string? email, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(email))
        {
            return errors;
        }

        // Simulate a 1.5-second server round-trip to check uniqueness
        await Task.Delay(1500, cancellationToken);

        if (TakenEmails.Contains(email))
        {
            errors.Add($"The email '{email}' is already registered.");
        }

        return errors;
    }

    private static async Task<List<string>> ValidateUsernameAsync(string? username, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(username))
        {
            return errors;
        }

        // Simulate a 2-second server round-trip to check uniqueness
        await Task.Delay(2000, cancellationToken);

        // Simulate a random infrastructure failure ~20% of the time for "blazor" prefix
        if (username.StartsWith("blaz", StringComparison.OrdinalIgnoreCase) && Random.Shared.Next(5) == 0)
        {
            throw new InvalidOperationException("Simulated server error checking username availability.");
        }

        if (TakenUsernames.Contains(username))
        {
            errors.Add($"The username '{username}' is already taken.");
        }

        return errors;
    }

    private static bool IsAsyncField(in FieldIdentifier fieldIdentifier)
        => fieldIdentifier.FieldName is nameof(Registration.Email) or nameof(Registration.Username);

    public void Dispose()
    {
        EditContext.OnFieldChanged -= OnFieldChanged;
        EditContext.OnValidationRequestedAsync -= OnValidationRequestedAsync;
        _messages.Clear();
        EditContext.NotifyValidationStateChanged();
    }
}
