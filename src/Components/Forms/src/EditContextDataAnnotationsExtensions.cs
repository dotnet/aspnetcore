// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Extension methods to add DataAnnotations validation to an <see cref="EditContext"/>.
    /// </summary>
    public static class EditContextDataAnnotationsExtensions
    {
        /// <summary>
        /// Adds DataAnnotations validation support to the <see cref="EditContext"/>.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        [Obsolete("Use " + nameof(EnableDataAnnotationsValidation) + " instead.")]
        public static EditContext AddDataAnnotationsValidation(this EditContext editContext)
        {
            EnableDataAnnotationsValidation(editContext);
            return editContext;
        }

        /// <summary>
        /// Enables DataAnnotations validation support for the <see cref="EditContext"/>.
        /// </summary>
        /// <param name="editContext">The <see cref="EditContext"/>.</param>
        /// <returns>A disposable object whose disposal will remove DataAnnotations validation support from the <see cref="EditContext"/>.</returns>
        public static IDisposable EnableDataAnnotationsValidation(this EditContext editContext)
        {
            return new DataAnnotationsEventSubscriptions(editContext);
        }

        private sealed class DataAnnotationsEventSubscriptions : IDisposable
        {
            private static readonly ConcurrentDictionary<(Type ModelType, string FieldName), PropertyInfo?> _propertyInfoCache = new();

            private readonly EditContext _editContext;
            private readonly ValidationMessageStore _messages;

            public DataAnnotationsEventSubscriptions(EditContext editContext)
            {
                if (editContext == null)
                {
                    throw new ArgumentNullException(nameof(editContext));
                }

                _editContext = editContext ?? throw new ArgumentNullException(nameof(editContext));
                _messages = new ValidationMessageStore(_editContext);

                _editContext.OnFieldChanged += OnFieldChanged;
                _editContext.OnValidationRequested += OnValidationRequested;
            }

            private void OnFieldChanged(object? sender, FieldChangedEventArgs eventArgs)
            {
                var fieldIdentifier = eventArgs.FieldIdentifier;
                if (TryGetValidatableProperty(fieldIdentifier, out var propertyInfo))
                {
                    var propertyValue = propertyInfo.GetValue(fieldIdentifier.Model);
                    var validationContext = new ValidationContext(fieldIdentifier.Model)
                    {
                        MemberName = propertyInfo.Name
                    };
                    var results = new List<ValidationResult>();

                    Validator.TryValidateProperty(propertyValue, validationContext, results);
                    _messages.Clear(fieldIdentifier);
                    _messages.Add(fieldIdentifier, results.Select(result => result.ErrorMessage!));

                    // We have to notify even if there were no messages before and are still no messages now,
                    // because the "state" that changed might be the completion of some async validation task
                    _editContext.NotifyValidationStateChanged();
                }
            }

            private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
            {
                var validationContext = new ValidationContext(_editContext.Model);
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(_editContext.Model, validationContext, validationResults, true);

                // Transfer results to the ValidationMessageStore
                _messages.Clear();
                foreach (var validationResult in validationResults)
                {
                    if (validationResult == null)
                    {
                        continue;
                    }

                    if (!validationResult.MemberNames.Any())
                    {
                        _messages.Add(new FieldIdentifier(_editContext.Model, fieldName: string.Empty), validationResult.ErrorMessage!);
                        continue;
                    }

                    foreach (var memberName in validationResult.MemberNames)
                    {
                        _messages.Add(_editContext.Field(memberName), validationResult.ErrorMessage!);
                    }
                }

                _editContext.NotifyValidationStateChanged();
            }

            public void Dispose()
            {
                _messages.Clear();
                _editContext.OnFieldChanged -= OnFieldChanged;
                _editContext.OnValidationRequested -= OnValidationRequested;
                _editContext.NotifyValidationStateChanged();
            }

            private static bool TryGetValidatableProperty(in FieldIdentifier fieldIdentifier, [NotNullWhen(true)] out PropertyInfo? propertyInfo)
            {
                var cacheKey = (ModelType: fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
                if (!_propertyInfoCache.TryGetValue(cacheKey, out propertyInfo))
                {
                    // DataAnnotations only validates public properties, so that's all we'll look for
                    // If we can't find it, cache 'null' so we don't have to try again next time
                    propertyInfo = cacheKey.ModelType.GetProperty(cacheKey.FieldName);

                    // No need to lock, because it doesn't matter if we write the same value twice
                    _propertyInfoCache[cacheKey] = propertyInfo;
                }

                return propertyInfo != null;
            }
        }
    }
}
