// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Components.Forms.Mapping;

internal static class EditContextFormMappingExtensions
{
    private static readonly object _key = new();

    public static IDisposable EnableFormMappingContextExtensions(this EditContext context, FormMappingContext mappingContext)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(mappingContext, nameof(mappingContext));

        context.Properties[_key] = mappingContext;

        return new MappingContextEventSubscriptions(context, mappingContext);
    }

    public static string? GetAttemptedValue(this EditContext context, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(fieldName, nameof(fieldName));

        if (context.Properties.TryGetValue(_key, out var result) && result is FormMappingContext mappingContext)
        {
            return mappingContext.GetAttemptedValue(fieldName);
        }

        return null;
    }

    private sealed class MappingContextEventSubscriptions : IDisposable
    {
        private readonly EditContext _editContext;
        private readonly FormMappingContext _mappingContext;
        private ValidationMessageStore? _messages;
        private bool _hasmessages;

        public MappingContextEventSubscriptions(EditContext editContext, FormMappingContext mappingContext)
        {
            _editContext = editContext;
            _mappingContext = mappingContext;

            _editContext.OnValidationRequested += OnValidationRequested;
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            if (_messages != null)
            {
                // We already added the messages from the mapping context,
                // we don't have to do anything.
                return;
            }

            _messages = new ValidationMessageStore(_editContext);
            var adddedMessages = false;
            foreach (var error in _mappingContext.GetAllErrors())
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
                // There were mapping errors, notify.
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
