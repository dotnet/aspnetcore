// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// A base class for form input components. This base class automatically
    /// integrates with an <see cref="Forms.EditContext"/>, which must be supplied
    /// as a cascading parameter.
    /// </summary>
    public abstract class InputBase<TValue> : ComponentBase, IDisposable
    {
        private readonly EventHandler<ValidationStateChangedEventArgs> _validationStateChangedHandler;
        private bool _previousParsingAttemptFailed;
        private ValidationMessageStore? _parsingValidationMessages;
        private Type? _nullableUnderlyingType;

        [CascadingParameter] EditContext CascadedEditContext { get; set; } = default!;

        /// <summary>
        /// Gets or sets a collection of additional attributes that will be applied to the created element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        /// <summary>
        /// Gets or sets the value of the input. This should be used with two-way binding.
        /// </summary>
        /// <example>
        /// @bind-Value="model.PropertyName"
        /// </example>
        [Parameter]
        public TValue? Value { get; set; }

        /// <summary>
        /// Gets or sets a callback that updates the bound value.
        /// </summary>
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets an expression that identifies the bound value.
        /// </summary>
        [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

        /// <summary>
        /// Gets or sets the display name for this field.
        /// <para>This value is used when generating error messages when the input value fails to parse correctly.</para>
        /// </summary>
        [Parameter] public string? DisplayName { get; set; }

        /// <summary>
        /// Gets the associated <see cref="Forms.EditContext"/>.
        /// </summary>
        protected EditContext EditContext { get; set; } = default!;

        /// <summary>
        /// Gets the <see cref="FieldIdentifier"/> for the bound value.
        /// </summary>
        protected internal FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the current value of the input.
        /// </summary>
        protected TValue? CurrentValue
        {
            get => Value;
            set
            {
                var hasChanged = !EqualityComparer<TValue>.Default.Equals(value, Value);
                if (hasChanged)
                {
                    Value = value;
                    _ = ValueChanged.InvokeAsync(Value);
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current value of the input, represented as a string.
        /// </summary>
        protected string? CurrentValueAsString
        {
            get => FormatValueAsString(CurrentValue);
            set
            {
                _parsingValidationMessages?.Clear();

                bool parsingFailed;

                if (_nullableUnderlyingType != null && string.IsNullOrEmpty(value))
                {
                    // Assume if it's a nullable type, null/empty inputs should correspond to default(T)
                    // Then all subclasses get nullable support almost automatically (they just have to
                    // not reject Nullable<T> based on the type itself).
                    parsingFailed = false;
                    CurrentValue = default!;
                }
                else if (TryParseValueFromString(value, out var parsedValue, out var validationErrorMessage))
                {
                    parsingFailed = false;
                    CurrentValue = parsedValue!;
                }
                else
                {
                    parsingFailed = true;

                    if (_parsingValidationMessages == null)
                    {
                        _parsingValidationMessages = new ValidationMessageStore(EditContext);
                    }

                    _parsingValidationMessages.Add(FieldIdentifier, validationErrorMessage);

                    // Since we're not writing to CurrentValue, we'll need to notify about modification from here
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }

                // We can skip the validation notification if we were previously valid and still are
                if (parsingFailed || _previousParsingAttemptFailed)
                {
                    EditContext.NotifyValidationStateChanged();
                    _previousParsingAttemptFailed = parsingFailed;
                }
            }
        }

        /// <summary>
        /// Constructs an instance of <see cref="InputBase{TValue}"/>.
        /// </summary>
        protected InputBase()
        {
            _validationStateChangedHandler = OnValidateStateChanged;
        }

        /// <summary>
        /// Formats the value as a string. Derived classes can override this to determine the formating used for <see cref="CurrentValueAsString"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <returns>A string representation of the value.</returns>
        protected virtual string? FormatValueAsString(TValue? value)
            => value?.ToString();

        /// <summary>
        /// Parses a string to create an instance of <typeparamref name="TValue"/>. Derived classes can override this to change how
        /// <see cref="CurrentValueAsString"/> interprets incoming values.
        /// </summary>
        /// <param name="value">The string value to be parsed.</param>
        /// <param name="result">An instance of <typeparamref name="TValue"/>.</param>
        /// <param name="validationErrorMessage">If the value could not be parsed, provides a validation error message.</param>
        /// <returns>True if the value could be parsed; otherwise false.</returns>
        protected abstract bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage);

        /// <summary>
        /// Gets a string that indicates the status of the field being edited. This will include
        /// some combination of "modified", "valid", or "invalid", depending on the status of the field.
        /// </summary>
        private string FieldClass
            => EditContext.FieldCssClass(FieldIdentifier);

        /// <summary>
        /// Gets a CSS class string that combines the <c>class</c> attribute and <see cref="FieldClass"/>
        /// properties. Derived components should typically use this value for the primary HTML element's
        /// 'class' attribute.
        /// </summary>
        protected string CssClass
        {
            get
            {
                if (AdditionalAttributes != null &&
                    AdditionalAttributes.TryGetValue("class", out var @class) &&
                    !string.IsNullOrEmpty(Convert.ToString(@class, CultureInfo.InvariantCulture)))
                {
                    return $"{@class} {FieldClass}";
                }

                return FieldClass; // Never null or empty
            }
        }


        /// <inheritdoc />
        public override Task SetParametersAsync(ParameterView parameters)
        {
            parameters.SetParameterProperties(this);

            if (EditContext == null)
            {
                // This is the first run
                // Could put this logic in OnInit, but its nice to avoid forcing people who override OnInit to call base.OnInit()

                if (CascadedEditContext == null)
                {
                    throw new InvalidOperationException($"{GetType()} requires a cascading parameter " +
                        $"of type {nameof(Forms.EditContext)}. For example, you can use {GetType().FullName} inside " +
                        $"an {nameof(EditForm)}.");
                }

                if (ValueExpression == null)
                {
                    throw new InvalidOperationException($"{GetType()} requires a value for the 'ValueExpression' " +
                        $"parameter. Normally this is provided automatically when using 'bind-Value'.");
                }

                EditContext = CascadedEditContext;
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                _nullableUnderlyingType = Nullable.GetUnderlyingType(typeof(TValue));

                EditContext.OnValidationStateChanged += _validationStateChangedHandler;
            }
            else if (CascadedEditContext != EditContext)
            {
                // Not the first run

                // We don't support changing EditContext because it's messy to be clearing up state and event
                // handlers for the previous one, and there's no strong use case. If a strong use case
                // emerges, we can consider changing this.
                throw new InvalidOperationException($"{GetType()} does not support changing the " +
                    $"{nameof(Forms.EditContext)} dynamically.");
            }

            UpdateAdditionalValidationAttributes();

            // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
            return base.SetParametersAsync(ParameterView.Empty);
        }

        private void OnValidateStateChanged(object? sender, ValidationStateChangedEventArgs eventArgs)
        {
            UpdateAdditionalValidationAttributes();

            StateHasChanged();
        }

        private void UpdateAdditionalValidationAttributes()
        {
            var hasAriaInvalidAttribute = AdditionalAttributes != null && AdditionalAttributes.ContainsKey("aria-invalid");
            if (EditContext.GetValidationMessages(FieldIdentifier).Any())
            {
                if (hasAriaInvalidAttribute)
                {
                    // Do not overwrite the attribute value
                    return;
                }

                if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                {
                    AdditionalAttributes = additionalAttributes;
                }

                // To make the `Input` components accessible by default
                // we will automatically render the `aria-invalid` attribute when the validation fails
                additionalAttributes["aria-invalid"] = true;
            }
            else if (hasAriaInvalidAttribute)
            {
                // No validation errors. Need to remove `aria-invalid` if it was rendered already

                if (AdditionalAttributes!.Count == 1)
                {
                    // Only aria-invalid argument is present which we don't need any more
                    AdditionalAttributes = null;
                }
                else
                {
                    if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
                    {
                        AdditionalAttributes = additionalAttributes;
                    }

                    additionalAttributes.Remove("aria-invalid");
                }
            }
        }

        /// <summary>
        /// Returns a dictionary with the same values as the specified <paramref name="source"/>.
        /// </summary>
        /// <returns>true, if a new dictrionary with copied values was created. false - otherwise.</returns>
        private bool ConvertToDictionary(IReadOnlyDictionary<string, object>? source, out Dictionary<string, object> result)
        {
            var newDictionaryCreated = true;
            if (source == null)
            {
                result = new Dictionary<string, object>();
            }
            else if (source is Dictionary<string, object> currentDictionary)
            {
                result = currentDictionary;
                newDictionaryCreated = false;
            }
            else
            {
                result = new Dictionary<string, object>();
                foreach (var item in source)
                {
                    result.Add(item.Key, item.Value);
                }
            }

            return newDictionaryCreated;
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
        }

        void IDisposable.Dispose()
        {
            EditContext.OnValidationStateChanged -= _validationStateChangedHandler;
            Dispose(disposing: true);
        }
    }
}
