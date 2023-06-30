// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Extension methods to add <see cref="ModelBindingContext"/> errors to an <see cref="EditContext"/>.
/// </summary>
internal static class EditContextBindingExtensions
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
        private ValidationMessageStore? _messages;
        private bool _hasmessages;

        public BindingContextEventSubscriptions(EditContext editContext, ModelBindingContext serviceProvider)
        {
            _editContext = editContext;
            _bindingContext = serviceProvider;

            _editContext.OnValidationRequested += OnValidationRequested;
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (_messages != null)
            {
                // We already added the messages from the binding context,
                // we don't have to do anything.
                return;
            }

            _messages = new ValidationMessageStore(_editContext);
            var adddedMessages = false;
            foreach (var error in _bindingContext.GetAllErrors())
            {
                var owner = error.Container;
                var key = error.Name;
                var errors = error.ErrorMessages;
                FieldIdentifier fieldIdentifier;
                fieldIdentifier = new FieldIdentifier(owner ?? _editContext.Model, key);

                foreach (var errorMessage in errors)
                {
                    adddedMessages = true;
                    // TODO: We need to support localizing the error message.
                    _messages.Add(fieldIdentifier, errorMessage.ToString(CultureInfo.CurrentCulture));
                    _hasmessages = true;
                }
            }

            if (adddedMessages)
            {
                // There were binding errors, notify.
                _editContext.NotifyValidationStateChanged();
            }
        }

        public void Dispose()
        {
            _messages?.Clear();
            _editContext.OnValidationRequested -= OnValidationRequested;
            if (_hasmessages)
            {
                _editContext.NotifyValidationStateChanged();
            }
        }
    }
}
