// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(Microsoft.AspNetCore.Components.Forms.EditContextDataAnnotationsExtensions))]

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Extension methods to add <see cref="ModelBindingContext"/> errors to an <see cref="EditContext"/>.
/// </summary>
public static class EditContextBindingExtensions
{
    private static readonly object _key = new();

    /// <summary>
    /// Enables <see cref="ModelBindingContext"/> errors to be added to the <see cref="EditContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="EditContext"/>.</param>
    /// <param name="bindingContext">The <see cref="ModelBindingContext"/>.</param>
    /// <returns></returns>
    public static IDisposable EnableBindingContextExtensions(this EditContext context, ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

        context.Properties[_key] = bindingContext;

        return new BindingContextEventSubscriptions(context, bindingContext);
    }

    /// <summary>
    /// Gets the attempted value for the specified field name.
    /// </summary>
    /// <param name="context">The <see cref="EditContext"/>.</param>
    /// <param name="fieldName">The field name.</param>
    /// <returns></returns>
    public static string? GetAttemptedValue(this EditContext context, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(fieldName, nameof(fieldName));

        if (context.Properties.TryGetValue(_key, out var result) && result is ModelBindingContext bindingContext)
        {
            return bindingContext.GetAttemptedValue(fieldName);
        }

        return null;
    }

    private sealed class BindingContextEventSubscriptions : IDisposable
    {
        private readonly EditContext _editContext;
        private readonly ModelBindingContext _bindingContext;
        private readonly ValidationMessageStore _messages;

        public BindingContextEventSubscriptions(EditContext editContext, ModelBindingContext serviceProvider)
        {
            _editContext = editContext;
            _bindingContext = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);

            _editContext.OnValidationRequested += OnValidationRequested;
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            foreach (var (key, errors) in _bindingContext.GetAllErrors())
            {
                var fieldIdentifier = key == "" ?
                    new FieldIdentifier(_editContext.Model, fieldName: string.Empty)
                    : _editContext.Field(key);

                foreach (var error in errors)
                {
                    // TODO: We need to support localizing the error message.
                    _messages.Add(fieldIdentifier, error.ToString(CultureInfo.InvariantCulture));
                }
            }

            _editContext.NotifyValidationStateChanged();
        }

        public void Dispose()
        {
            _messages.Clear();
            _editContext.OnValidationRequested -= OnValidationRequested;
            _editContext.NotifyValidationStateChanged();
        }
    }
}
