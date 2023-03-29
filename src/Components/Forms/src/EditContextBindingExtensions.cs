// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Forms;

public static class EditContextBindingExtensions
{
    public static IDisposable EnableBindingContextExtensions(this EditContext context, BindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(bindingContext, nameof(bindingContext));

        return new BindingContextEventSubscriptions(context, bindingContext);
    }

    private sealed class BindingContextEventSubscriptions : IDisposable
    {
        private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();

        private readonly EditContext _editContext;
        private readonly BindingContext _bindingContext;
        private readonly ValidationMessageStore _messages;

        public BindingContextEventSubscriptions(EditContext editContext, BindingContext serviceProvider)
        {
            _editContext = editContext;
            _bindingContext = serviceProvider;
            _messages = new ValidationMessageStore(_editContext);

            _editContext.OnValidationRequested += OnValidationRequested;
        }

        private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        {
            _messages.Clear();
            foreach (var (key, errors) in _bindingContext.BindingErrors)
            {
                var fieldIdentifier = !(key == "") ?
                    new FieldIdentifier(_editContext.Model, fieldName: string.Empty)
                    : _editContext.Field(key);

                foreach (var error in errors)
                {
                    _messages.Add(fieldIdentifier, error);
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
